using ECommerceManagement.Application.DTOs.Orders;
using ECommerceManagement.Application.Interfaces;
using ECommerceManagement.Domain.Entities;
using ECommerceManagement.Domain.Enums;
using SysmondAx.Integration.Models.Dtos;
using SysmondAx.Integration.Services.Order;
using SysmondAx.Integration.Services.Stock; 

namespace ECommerceManagement.Application.Services;

public class CustomerOrderService : ICustomerOrderService
{
    private readonly IGenericRepository<Order> _orderRepository;
    private readonly IGenericRepository<Product> _productRepository;
    private readonly IGenericRepository<Address> _addressRepository;
    private readonly IGenericRepository<ProductMovement> _productMovementRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISysmondOrderService _sysmondOrderService;
    private readonly ISysmondStockService _sysmondStockService;

    public CustomerOrderService(
        IGenericRepository<Order> orderRepository,
        IGenericRepository<Product> productRepository,
        IGenericRepository<Address> addressRepository,
        IGenericRepository<ProductMovement> productMovementRepository,
        IUnitOfWork unitOfWork,
        ISysmondOrderService sysmondOrderService,
        ISysmondStockService sysmondStockService)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _addressRepository = addressRepository;
        _productMovementRepository = productMovementRepository;
        _unitOfWork = unitOfWork;
        _sysmondOrderService = sysmondOrderService;
        _sysmondStockService = sysmondStockService;
    }

    public async Task<IEnumerable<OrderDto>> GetMyOrdersAsync(int customerId)
    {
        var orders = await _orderRepository.GetWhereAsync(
            o => o.CustomerId == customerId,
            o => o.Customer, 
            o => o.OrderItems
        );

        var orderList = orders.ToList();

        var pendingSysmondOrderIds = orderList
            .Where(o => o.Status == OrderStatus.Pending && o.SysmondOrderId.HasValue)
            .Select(o => o.SysmondOrderId!.Value)
            .ToList();

        if (pendingSysmondOrderIds.Any())
        {
            var sysmondStatuses = await _sysmondOrderService.GetOrderStatusesByIdsAsync(pendingSysmondOrderIds);

            bool isAnyStatusChanged = false;

            foreach (var localOrder in orderList.Where(o => o.SysmondOrderId.HasValue && pendingSysmondOrderIds.Contains(o.SysmondOrderId.Value)))
            {
                var sysmondOrder = sysmondStatuses.FirstOrDefault(s => s.Id == localOrder.SysmondOrderId);
                if (sysmondOrder != null)
                {
                    OrderStatus newStatus = localOrder.Status;
                
                    if (sysmondOrder.Status == 20) // Approved (Onaylandı)
                        newStatus = OrderStatus.Invoiced; 
                    else if (sysmondOrder.Status == -100) // Cancelled (İptal)
                        newStatus = OrderStatus.Canceled;
                
                    if (newStatus != localOrder.Status)
                    {
                        localOrder.Status = newStatus;
                        localOrder.UpdatedAt = DateTime.UtcNow;
                        _orderRepository.Update(localOrder);
                        isAnyStatusChanged = true;
                    }
                }
            }

            if (isAnyStatusChanged)
            {
                await _unitOfWork.SaveChangesAsync();
            }
        }

        var productIds = orderList.SelectMany(o => o.OrderItems.Select(item => item.ProductId)).Distinct().ToList();
        var products = await _productRepository.GetWhereAsync(p => productIds.Contains(p.Id));
        var productDict = products.ToDictionary(p => p.Id, p => p.Name);

        return orderList.Select(o => new OrderDto
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
        }).OrderByDescending(o => o.CreatedAt);
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

            var draftDto = new SysmondOrderDraftCreateDto
            {
                DocNo = $"ORD-{newOrder.Id}", 
                OrderDate = DateTime.UtcNow
            };
            var sysmondOrderId = await _sysmondOrderService.CreateDraftOrderAsync(draftDto);
            
            newOrder.SysmondOrderId = sysmondOrderId;

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

                if (product.SysmondStockId.HasValue)
                {
                    var priceDto = await _sysmondStockService.GetStockPriceAsync(product.SysmondStockId.Value);

                    var orderItemDto = new SysmondOrderItemCreateDto
                    {
                        OrderId = sysmondOrderId,
                        StockId = product.SysmondStockId.Value,
                        StockPriceId = priceDto?.Id,
                        Quantity = reqQty,
                        UnitPrice = product.Price,
                        Code = product.Sku,
                        Name = product.Name
                    };

                    await _sysmondOrderService.AddOrderItemAsync(orderItemDto);
                }
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

        // 1. Sysmond Tarafındaki Siparişin Statüsünü İptal (-100) Olarak Güncelle
        if (order.SysmondOrderId.HasValue)
        {
            var statusUpdateDto = new SysmondOrderStatusUpdateDto
            {
                Id = order.SysmondOrderId.Value,
                Status = -100, // Sysmond İptal Statüsü (-100 = Cancelled)
                StatusNote = "Müşteri tarafından web uygulaması üzerinden iptal edildi."
            };

            try
            {
                await _sysmondOrderService.UpdateOrderStatusAsync(statusUpdateDto);
            }
            catch (Exception ex)
            {
                throw new Exception($"Sipariş iptali Sysmond tarafında reddedildi. Detay: {ex.Message}");
            }
        }

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
    
    
    public async Task SyncOrdersFromSysmondAsync()
    {
        var sysmondOrders = await _sysmondOrderService.GetAllOrdersAsync();

        var localOrders = await _orderRepository.GetAllAsync(o => o.OrderItems);
        var localProducts = await _productRepository.GetAllAsync(); // Ürün eşleştirmesi için

        bool hasChanges = false;

        foreach (var sysOrder in sysmondOrders)
        {
            var existingOrder = localOrders.FirstOrDefault(o => o.SysmondOrderId == sysOrder.Id);

            OrderStatus mappedStatus = sysOrder.Status switch
            {
                -100 => OrderStatus.Canceled,
                10 => OrderStatus.Pending,
                20 => OrderStatus.Invoiced,
                30 => OrderStatus.Shipped, // PartiallyDelivered
                40 => OrderStatus.Delivered,
                _ => OrderStatus.Pending
            };

            if (existingOrder != null)
            {
                if (existingOrder.Status != mappedStatus)
                {
                    existingOrder.Status = mappedStatus;
                    existingOrder.UpdatedAt = DateTime.UtcNow;
                    _orderRepository.Update(existingOrder);
                    hasChanges = true;
                }
            }
            else
            {
                var newOrder = new Order
                {
                    SysmondOrderId = sysOrder.Id,
                    CustomerId = 1,       
                    SellerId = 1,        
                    ShippingAddressId = 1,
                    BillingAddressId = 1, 
                    TotalAmount = sysOrder.Total,
                    Status = mappedStatus,
                    CreatedAt = sysOrder.CreatedOn,
                    OrderItems = new List<OrderItem>()
                };

                var sysItems = await _sysmondOrderService.GetOrderItemsAsync(sysOrder.Id);

                foreach (var sItem in sysItems)
                {
                    var localProduct = localProducts.FirstOrDefault(p => p.SysmondStockId == sItem.StockId);
                    
                    if (localProduct != null)
                    {
                        newOrder.OrderItems.Add(new OrderItem
                        {
                            ProductId = localProduct.Id,
                            Quantity = (int)sItem.Quantity,
                            UnitPrice = sItem.UnitPrice,
                            LineTotal = sItem.UnitPrice * (int)sItem.Quantity,
                            SysmondOrderItemId = sItem.Id
                        });
                    }
                }

                await _orderRepository.AddAsync(newOrder);
                hasChanges = true;
            }
        }

        var sysmondOrderIds = sysmondOrders.Select(s => s.Id).ToHashSet();

        foreach (var localOrder in localOrders)
        {
            if (localOrder.SysmondOrderId.HasValue && !sysmondOrderIds.Contains(localOrder.SysmondOrderId.Value))
            {
                _orderRepository.Delete(localOrder);
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            await _unitOfWork.SaveChangesAsync();
        }
    }
    
}