using AutoMapper;
using ECommerceManagement.Application.DTOs.Invoices;
using ECommerceManagement.Application.DTOs.Orders;
using ECommerceManagement.Application.Interfaces;
using ECommerceManagement.Domain.Entities;
using ECommerceManagement.Domain.Enums;
using SysmondAx.Integration.Models.Dtos;
using SysmondAx.Integration.Services.Invoice;
using SysmondAx.Integration.Services.Order;
using SysmondAx.Integration.Services.Stock;

namespace ECommerceManagement.Application.Services;

public class SellerOrderService : ISellerOrderService
{
    private readonly IGenericRepository<Order> _orderRepository;
    private readonly IGenericRepository<Invoice> _invoiceRepository;
    private readonly IGenericRepository<Customer> _customerRepository;
    private readonly IGenericRepository<Product> _productRepository;
    private readonly IGenericRepository<ProductMovement> _productMovementRepository;
    private readonly ISysmondOrderService _sysmondOrderService;
    private readonly ISysmondInvoiceService _sysmondInvoiceService;
    private readonly ISysmondStockService _sysmondStockService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public SellerOrderService(
        IGenericRepository<Order> orderRepository,
        IGenericRepository<Invoice> invoiceRepository,
        IGenericRepository<Customer> customerRepository,
        IGenericRepository<Product> productRepository,
        IGenericRepository<ProductMovement> productMovementRepository,
        ISysmondOrderService sysmondOrderService,
        ISysmondInvoiceService sysmondInvoiceService,
        ISysmondStockService sysmondStockService,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _orderRepository = orderRepository;
        _invoiceRepository = invoiceRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _productMovementRepository = productMovementRepository;
        _sysmondOrderService = sysmondOrderService;
        _sysmondInvoiceService = sysmondInvoiceService;
        _sysmondStockService = sysmondStockService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IEnumerable<OrderDto>> GetPendingOrdersAsync(int sellerId)
    {
        // 1. Lokal veritabanından satıcının sadece "Bekleyen (Pending)" siparişlerini çek
        var orders = await _orderRepository.GetWhereAsync(
            o => o.SellerId == sellerId && o.Status == OrderStatus.Pending,
            o => o.Customer, 
            o => o.OrderItems
        );

        var orderList = orders.ToList();

        // 2. Sysmond Entegrasyonu: Sysmond ID'si olanları topla
        var pendingSysmondOrderIds = orderList
            .Where(o => o.SysmondOrderId.HasValue)
            .Select(o => o.SysmondOrderId!.Value)
            .ToList();

        if (pendingSysmondOrderIds.Any())
        {
            // 3. Sysmond'dan bu siparişlerin güncel durumlarını çek
            var sysmondStatuses = await _sysmondOrderService.GetOrderStatusesByIdsAsync(pendingSysmondOrderIds);

            bool isAnyStatusChanged = false;
            var ordersToRemoveFromList = new List<Order>();

            foreach (var localOrder in orderList.Where(o => o.SysmondOrderId.HasValue))
            {
                var sysmondOrder = sysmondStatuses.FirstOrDefault(s => s.Id == localOrder.SysmondOrderId.Value);
                if (sysmondOrder != null)
                {
                    // Sysmond Statü Dönüşümü
                    OrderStatus newStatus = localOrder.Status;
                    
                    if (sysmondOrder.Status == 20) // Approved (Onaylandı)
                        newStatus = OrderStatus.Invoiced; 
                    else if (sysmondOrder.Status == -100) // Cancelled (İptal)
                        newStatus = OrderStatus.Canceled;
                    
                    // Eğer statü Pending'den başka bir duruma geçmişse
                    if (newStatus != localOrder.Status)
                    {
                        localOrder.Status = newStatus;
                        localOrder.UpdatedAt = DateTime.UtcNow;
                        _orderRepository.Update(localOrder);
                        isAnyStatusChanged = true;
                        
                        // Artık 'Pending' olmadığı için bu siparişi döneceğimiz listeden çıkarmalıyız
                        ordersToRemoveFromList.Add(localOrder);
                    }
                }
            }

            // Değişiklik varsa veritabanına kaydet ve güncellenenleri listeden temizle
            if (isAnyStatusChanged)
            {
                await _unitOfWork.SaveChangesAsync();
                
                foreach (var orderToRemove in ordersToRemoveFromList)
                {
                    orderList.Remove(orderToRemove);
                }
            }
        }

        // 4. Geriye kalan (gerçekten hala Pending olan) siparişleri DTO'ya çevir
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
        }).OrderByDescending(o => o.CreatedAt); // En yeni bekleyen siparişler en üstte görünsün
    }

    public async Task<InvoiceDto> CreateAndConfirmInvoiceAsync(int orderId, int sellerId)
    {
        // 1. Siparişi kalemleriyle çek
        var order = await _orderRepository.GetByIdAsync(orderId, o => o.OrderItems);
        if (order == null || order.SellerId != sellerId)
            throw new KeyNotFoundException("Sipariş bulunamadı veya bu satıcıya ait değil.");

        if (order.Status != OrderStatus.Pending)
            throw new InvalidOperationException("Yalnızca bekleyen (Pending) siparişler faturalandırılabilir.");

        // 2. Müşteriyi çek
        var customer = await _customerRepository.GetByIdAsync(order.CustomerId);
        string customerFullName = customer != null ? $"{customer.FirstName} {customer.LastName}" : "Bilinmeyen Müşteri";

        // 3. Lokal Fatura Hazırlığı
        var invoice = new Invoice
        {
            OrderId = order.Id,
            SellerId = order.SellerId,
            CustomerName = customerFullName,
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{order.Id}",
            TotalAmount = order.TotalAmount,
            Status = InvoiceStatus.Confirmed,
            AxIntegrationStatus = AxIntegrationStatus.Sent,
            CreatedAt = DateTime.UtcNow,
            InvoiceItems = new List<InvoiceItem>()
        };

        var invoiceDtoItems = new List<InvoiceItemDto>();

        // 4. SYSMOND ENTEGRASYONU 
        if (order.SysmondOrderId.HasValue)
        {
            try
            {
                // A. Sysmond Siparişini Onayla (Status: 20)
                await _sysmondOrderService.UpdateOrderStatusAsync(new SysmondOrderStatusUpdateDto
                {
                    Id = order.SysmondOrderId.Value,
                    Status = 20, 
                    StatusNote = "Fatura kesildi ve sipariş onaylandı."
                });

                // B. Sysmond'da Fatura Taslağı Oluştur
                var draftInvoiceDto = new SysmondInvoiceDraftCreateDto
                {
                    ActId = Guid.Parse("1d15a962-3a17-f783-f15d-3a22e2d1b4b5"), 
                    CompanyPeriodId = Guid.Parse("e04ebee4-bdca-b9f1-ed45-3a22008d01a1"),
                    ActAddressId = Guid.Parse("8601d689-1e44-0ad3-65a6-3a22e2d1b4f4"),
                    CompanyAddressId = Guid.Parse("8849b16c-cb0b-e039-fe00-3a22008d01af"),
                    CompanyContactAddressId = Guid.Parse("234d43dc-9ed8-d462-e743-3a22008d01a9"),
                    TemplateId = Guid.Parse("bff0435b-f9f6-695d-41df-3a22008d01a9"),
                    IssueDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    OrderDocRefs = new List<SysmondInvoiceOrderDocRefDto>
                    {
                        new SysmondInvoiceOrderDocRefDto { OrderId = order.SysmondOrderId.Value }
                    }
                };
                var sysmondInvoiceId = await _sysmondInvoiceService.CreateDraftInvoiceAsync(draftInvoiceDto);

                // C. Kalemleri İşle
                foreach (var orderItem in order.OrderItems)
                {
                    var product = await _productRepository.GetByIdAsync(orderItem.ProductId);
                    
                    // Lokale Ekle
                    invoice.InvoiceItems.Add(new InvoiceItem
                    {
                        ProductId = orderItem.ProductId,
                        Quantity = orderItem.Quantity,
                        UnitPrice = orderItem.UnitPrice,
                        TaxRate = 20m, 
                        LineTotal = orderItem.LineTotal
                    });

                    // Response JSON için Listeye Ekle
                    invoiceDtoItems.Add(new InvoiceItemDto
                    {
                        ProductName = product?.Name ?? "Ürün",
                        Quantity = orderItem.Quantity,
                        UnitPrice = orderItem.UnitPrice,
                        TaxRate = 20m,
                        LineTotal = orderItem.LineTotal
                    });

                    // Sysmond StockPriceId Değerini Çek (Zorunlu Alan)
                    Guid? stockPriceId = null;
                    if (product?.SysmondStockId.HasValue == true)
                    {
                        var priceDto = await _sysmondStockService.GetStockPriceAsync(product.SysmondStockId.Value);
                        stockPriceId = priceDto?.Id;
                    }
                    
                    // Fallback: Eğer servisten gelmezse örnek payload'daki geçerli ID'yi kullan
                    stockPriceId ??= Guid.Parse("709c81f7-a2e2-94d5-ae24-3a2302bea9d5");

                    // Sysmond'a Kalem Ekle
                    await _sysmondInvoiceService.AddInvoiceItemAsync(new SysmondInvoiceItemCreateDto
                    {
                        InvoiceId = sysmondInvoiceId,
                        StockId = product?.SysmondStockId,
                        StockPriceId = stockPriceId, // <-- ZORUNLU ALAN DOLDURULDU
                        Name = product?.Name ?? "Ürün",
                        Code = product?.Sku ?? "123123",
                        MeasureUnitId = Guid.Parse("ec2118e6-8154-7926-e418-3a2194605ce0"),
                        WarehouseId = Guid.Parse("9004703d-3c8b-fd59-d77c-3a22008d032d"),
                        Quantity = orderItem.Quantity,
                        UnitPrice = orderItem.UnitPrice
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Sysmond'da fatura işlemi yapılırken hata oluştu: {ex.Message}");
            }
        }

        // 5. LOKAL VERİTABANINA KAYDET
        order.Status = OrderStatus.Invoiced;
        order.UpdatedAt = DateTime.UtcNow;

        await _invoiceRepository.AddAsync(invoice);
        _orderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync();

        // 6. JSON Yanıtını Dön
        var resultDto = _mapper.Map<InvoiceDto>(invoice);
        resultDto.CustomerName = customerFullName;
        resultDto.Items = invoiceDtoItems;

        return resultDto;
    }

    public async Task ShipOrderAsync(int orderId, int sellerId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null || order.SellerId != sellerId)
            throw new KeyNotFoundException("Sipariş bulunamadı.");

        if (order.Status != OrderStatus.Invoiced)
            throw new InvalidOperationException("Sadece faturası kesilmiş (Invoiced) siparişler kargolanabilir.");

        order.Status = OrderStatus.Shipped;
        order.UpdatedAt = DateTime.UtcNow;

        _orderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task CancelOrderAsync(int orderId, int sellerId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, o => o.OrderItems);
        if (order == null || order.SellerId != sellerId)
            throw new KeyNotFoundException("Sipariş bulunamadı.");

        if (order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Delivered)
            throw new InvalidOperationException("Kargolanmış veya teslim edilmiş siparişler iptal edilemez.");

        if (order.Status == OrderStatus.Canceled)
            throw new InvalidOperationException("Zaten iptal edilmiş bir sipariş tekrar iptal edilemez.");

        order.Status = OrderStatus.Canceled;
        order.UpdatedAt = DateTime.UtcNow;
        _orderRepository.Update(order);

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

        var invoice = await _invoiceRepository.GetAsync(i => i.OrderId == orderId);
        if (invoice != null)
        {
            invoice.Status = InvoiceStatus.Canceled;
            _invoiceRepository.Update(invoice);
        }

        await _unitOfWork.SaveChangesAsync();
    }
    
    public async Task<IEnumerable<OrderDto>> GetAllOrdersBySellerIdAsync(int sellerId)
    {
        var orders = await _orderRepository.GetWhereAsync(
            o => o.SellerId == sellerId,
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

    public async Task<IEnumerable<InvoiceDto>> GetInvoicesBySellerIdAsync(int sellerId)
    {
        var sellerInvoices = await _invoiceRepository.GetWhereAsync(i => i.SellerId == sellerId);
        
        return _mapper.Map<IEnumerable<InvoiceDto>>(sellerInvoices);
    }
}