using ECommerceManagement.Application.DTOs.Orders;
using ECommerceManagement.Application.Interfaces;
using ECommerceManagement.Domain.Entities;
using ECommerceManagement.Domain.Enums;

namespace ECommerceManagement.Application.Services;

public class CustomerOrderService : ICustomerOrderService
{
    private readonly IGenericRepository<Order> _orderRepository;
    private readonly IGenericRepository<Product> _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CustomerOrderService(
        IGenericRepository<Order> orderRepository,
        IGenericRepository<Product> productRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<OrderDto>> GetMyOrdersAsync(int customerId)
    {
        // 1. Siparişleri, müşteriyi ve sipariş kalemlerini çekiyoruz
        var orders = await _orderRepository.GetAllAsync(
            o => o.Customer, 
            o => o.OrderItems
        );

        // 2. Ürün adlarını eşleştirebilmek için sistemdeki tüm ürünleri çekip sözlüğe (Dictionary) atıyoruz
        var products = await _productRepository.GetAllAsync();
        var productDict = products.ToDictionary(p => p.Id, p => p.Name);

        return orders
            .Where(o => o.CustomerId == customerId)
            .Select(o => new OrderDto
            {
                Id = o.Id,
                CustomerId = o.CustomerId,
                CustomerFullName = o.Customer != null ? $"{o.Customer.FirstName} {o.Customer.LastName}" : string.Empty,
                SellerId = o.SellerId,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                CreatedAt = o.CreatedAt,
                Items = o.OrderItems.Select(item => new OrderItemDto
                {
                    ProductId = item.ProductId,
                    // Sözlükten ProductId'ye karşılık gelen ismi çekiyoruz!
                    ProductName = productDict.TryGetValue(item.ProductId, out var prodName) ? prodName : string.Empty,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    LineTotal = item.LineTotal
                }).ToList()
            });
    }

    public async Task CreateOrderAsync(CreateOrderDto dto)
    {
        if (!dto.Items.Any())
            throw new ArgumentException("Sepet boş olamaz.");

        // 1. Ürünleri DB'den çek ve stok kontrolü yap
        var allProducts = await _productRepository.GetAllAsync();
        var orderProducts = new List<(Product Entity, int RequestedQuantity)>();

        foreach (var item in dto.Items)
        {
            var product = allProducts.FirstOrDefault(p => p.Id == item.ProductId);
            
            if (product == null || !product.IsActive)
                throw new Exception($"Ürün bulunamadı veya satıştan kaldırılmış. (ID: {item.ProductId})");
                
            if (product.Quantity < item.Quantity)
                throw new Exception($"'{product.Name}' ürünü için yeterli stok yok. Mevcut: {product.Quantity}");

            orderProducts.Add((product, item.Quantity));
        }

        // 2. Sepetteki ürünleri SATICILARA GÖRE GRUPLA (Pazaryeri Mantığı)
        var groupedBySeller = orderProducts.GroupBy(p => p.Entity.SellerId);

        foreach (var sellerGroup in groupedBySeller)
        {
            var sellerId = sellerGroup.Key;
            decimal totalAmount = 0;
            var orderItems = new List<OrderItem>();

            foreach (var item in sellerGroup)
            {
                var product = item.Entity;
                var reqQty = item.RequestedQuantity;
                var lineTotal = product.Price * reqQty;
                totalAmount += lineTotal;

                // Stok Düşme İşlemi (Memory'de güncellenir)
                product.Quantity -= reqQty;
                _productRepository.Update(product);

                orderItems.Add(new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = reqQty,
                    UnitPrice = product.Price,
                    LineTotal = lineTotal
                });
            }

            // O satıcı için yeni bir sipariş oluşturulur
            var newOrder = new Order
            {
                CustomerId = dto.CustomerId,
                SellerId = sellerId,
                ShippingAddressId = dto.ShippingAddressId,
                BillingAddressId = dto.BillingAddressId,
                TotalAmount = totalAmount,
                Status = OrderStatus.Pending, // Satıcıya düşmesi için bekliyor
                OrderItems = orderItems
            };

            await _orderRepository.AddAsync(newOrder);
        }

        // 3. UnitOfWork ile tüm stok güncellemelerini ve yeni siparişleri tek Transaction'da kaydet
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task CancelMyOrderAsync(int orderId, int customerId)
    {
        // 1. Siparişi çekerken OrderItems tablosunu da Include ediyoruz!
        var order = await _orderRepository.GetByIdAsync(orderId, o => o.OrderItems);
    
        if (order == null || order.CustomerId != customerId)
            throw new KeyNotFoundException("Sipariş bulunamadı.");

        if (order.Status != OrderStatus.Pending)
            throw new InvalidOperationException("Bu sipariş iptal edilemez (Faturası kesilmiş veya kargolanmış olabilir).");

        order.Status = OrderStatus.Canceled;
        order.UpdatedAt = DateTime.UtcNow;
    
        // 2. STOK İADE ALGORİTMASI (Rollback)
        foreach (var item in order.OrderItems)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId);
            if (product != null)
            {
                // Satın alınan miktar kadar stoğu tekrar ürünün üstüne ekliyoruz
                product.Quantity += item.Quantity;
                _productRepository.Update(product);
            }
        }
    
        _orderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync();
    }
}