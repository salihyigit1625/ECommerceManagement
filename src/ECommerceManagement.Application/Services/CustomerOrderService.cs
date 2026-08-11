using ECommerceManagement.Application.DTOs.Orders;
using ECommerceManagement.Application.Interfaces;
using ECommerceManagement.Domain.Entities;
using ECommerceManagement.Domain.Enums;

namespace ECommerceManagement.Application.Services;

public class CustomerOrderService : ICustomerOrderService
{
    private readonly IGenericRepository<Order> _orderRepository;
    private readonly IGenericRepository<Product> _productRepository;
    private readonly IGenericRepository<Address> _addressRepository;
    private readonly IGenericRepository<ProductMovement> _productMovementRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CustomerOrderService(
        IGenericRepository<Order> orderRepository,
        IGenericRepository<Product> productRepository,
        IGenericRepository<Address> addressRepository,
        IGenericRepository<ProductMovement> productMovementRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _addressRepository = addressRepository;
        _productMovementRepository = productMovementRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<OrderDto>> GetMyOrdersAsync(int customerId)
    {
        var orders = await _orderRepository.GetWhereAsync(
            o => o.CustomerId == customerId,
            o => o.Customer, 
            o => o.OrderItems
        );

        var productIds = orders.SelectMany(o => o.OrderItems.Select(item => item.ProductId)).Distinct().ToList();
        var products = await _productRepository.GetWhereAsync(p => productIds.Contains(p.Id));
        var productDict = products.ToDictionary(p => p.Id, p => p.Name);

        return orders.Select(o => new OrderDto
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

        var shippingAddress = await _addressRepository.GetByIdAsync(dto.ShippingAddressId);
        if (shippingAddress == null || shippingAddress.CustomerId != dto.CustomerId || !shippingAddress.IsShipping)
            throw new InvalidOperationException("Geçersiz, size ait olmayan veya teslimat için uygun olmayan bir adres seçildi.");

        var billingAddress = await _addressRepository.GetByIdAsync(dto.BillingAddressId);
        if (billingAddress == null || billingAddress.CustomerId != dto.CustomerId || !billingAddress.IsBilling)
            throw new InvalidOperationException("Geçersiz, size ait olmayan veya fatura için uygun olmayan bir adres seçildi.");
        var productIds = dto.Items.Select(i => i.ProductId).ToList();
        var allProducts = await _productRepository.GetWhereAsync(p => productIds.Contains(p.Id));
        
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

        var groupedBySeller = orderProducts.GroupBy(p => p.Entity.SellerId);

        foreach (var sellerGroup in groupedBySeller)
        {
            var sellerId = sellerGroup.Key;
            decimal totalAmount = 0;
            var orderItems = new List<OrderItem>();

            var newOrder = new Order
            {
                CustomerId = dto.CustomerId,
                SellerId = sellerId,
                ShippingAddressId = dto.ShippingAddressId,
                BillingAddressId = dto.BillingAddressId,
                TotalAmount = 0,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _orderRepository.AddAsync(newOrder);
            await _unitOfWork.SaveChangesAsync(); 

            foreach (var item in sellerGroup)
            {
                var product = item.Entity;
                var reqQty = item.RequestedQuantity;
                var lineTotal = product.Price * reqQty;
                totalAmount += lineTotal;

                product.Quantity -= reqQty;
                _productRepository.Update(product);

                orderItems.Add(new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = reqQty,
                    UnitPrice = product.Price,
                    LineTotal = lineTotal
                });

                await _productMovementRepository.AddAsync(new ProductMovement
                {
                    ProductId = product.Id, 
                    MovementType = MovementType.Exit,
                    Quantity = reqQty,
                    ReferenceId = newOrder.Id, 
                    CreatedAt = DateTime.UtcNow
                });
            }

            newOrder.TotalAmount = totalAmount;
            newOrder.OrderItems = orderItems;
            
            _orderRepository.Update(newOrder); 
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task CancelMyOrderAsync(int orderId, int customerId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, o => o.OrderItems);
    
        if (order == null || order.CustomerId != customerId)
            throw new KeyNotFoundException("Sipariş bulunamadı.");

        if (order.Status != OrderStatus.Pending)
            throw new InvalidOperationException("Bu sipariş iptal edilemez (Faturası kesilmiş veya kargolanmış olabilir).");

        order.Status = OrderStatus.Canceled;
        order.UpdatedAt = DateTime.UtcNow;
    
        foreach (var item in order.OrderItems)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId);
            if (product != null)
            {
                product.Quantity += item.Quantity;
                _productRepository.Update(product);

                await _productMovementRepository.AddAsync(new ProductMovement
                {
                    ProductId = product.Id,
                    MovementType = MovementType.Entry,
                    Quantity = item.Quantity,
                    ReferenceId = order.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
    
        _orderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync();
    }
}