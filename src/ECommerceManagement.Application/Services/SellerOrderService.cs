using ECommerceManagement.Application.DTOs.Invoices;
using ECommerceManagement.Application.DTOs.Orders;
using ECommerceManagement.Application.Interfaces;
using ECommerceManagement.Domain.Entities;
using ECommerceManagement.Domain.Enums;

namespace ECommerceManagement.Application.Services;

public class SellerOrderService : ISellerOrderService
{
    private readonly IGenericRepository<Order> _orderRepository;
    private readonly IGenericRepository<Invoice> _invoiceRepository;
    private readonly IGenericRepository<Customer> _customerRepository;
    private readonly IGenericRepository<Product> _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SellerOrderService(
        IGenericRepository<Order> orderRepository,
        IGenericRepository<Invoice> invoiceRepository,
        IGenericRepository<Customer> customerRepository,
        IGenericRepository<Product> productRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _invoiceRepository = invoiceRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    // 1. Satıcı sadece kendisine gelen Pending siparişleri listeler
    public async Task<IEnumerable<OrderDto>> GetPendingOrdersAsync(int sellerId)
    {
        // 1. Siparişleri, müşteriyi ve sipariş kalemlerini Include ile çekiyoruz
        var orders = await _orderRepository.GetAllAsync(
            o => o.Customer, 
            o => o.OrderItems
        );

        // 2. Ürün adlarını eşleştirmek için ürünleri sözlüğe (Dictionary) alıyoruz
        var products = await _productRepository.GetAllAsync();
        var productDict = products.ToDictionary(p => p.Id, p => p.Name);

        return orders
            .Where(o => o.SellerId == sellerId && o.Status == OrderStatus.Pending)
            .Select(o => new OrderDto
            {
                Id = o.Id,
                CustomerId = o.CustomerId,
                // Müşteri ad soyad bilgisini dolduruyoruz
                CustomerFullName = o.Customer != null ? $"{o.Customer.FirstName} {o.Customer.LastName}" : string.Empty,
                SellerId = o.SellerId,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                CreatedAt = o.CreatedAt,
                // Sipariş kalemlerini ve ürün adlarını dolduruyoruz
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

    // 2. Fatura Taslağı Oluşturma (InvoiceStatus = Waiting)
    public async Task<InvoiceDto> CreateInvoiceDraftAsync(int orderId, int sellerId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null || order.SellerId != sellerId)
            throw new KeyNotFoundException("Sipariş bulunamadı veya bu satıcıya ait değil.");

        if (order.Status != OrderStatus.Pending)
            throw new InvalidOperationException("Sadece 'Pending' durumundaki siparişlere fatura kesilebilir.");

        // Müşteri adını anlık görüntü (Snapshot) olarak çekiyoruz
        var customer = await _customerRepository.GetByIdAsync(order.CustomerId);
        string customerFullName = customer != null ? $"{customer.FirstName} {customer.LastName}" : "Bilinmeyen Müşteri";

        var invoice = new Invoice
        {
            OrderId = order.Id,
            SellerId = sellerId,
            CustomerName = customerFullName,
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{order.Id}",
            TotalAmount = order.TotalAmount,
            Status = InvoiceStatus.Waiting,
            AxIntegrationStatus = AxIntegrationStatus.Pending
        };

        await _invoiceRepository.AddAsync(invoice);
        await _unitOfWork.SaveChangesAsync();

        return new InvoiceDto
        {
            Id = invoice.Id,
            OrderId = invoice.OrderId,
            InvoiceNumber = invoice.InvoiceNumber,
            CustomerName = invoice.CustomerName,
            TotalAmount = invoice.TotalAmount,
            Status = invoice.Status,
            AxIntegrationStatus = invoice.AxIntegrationStatus,
            CreatedAt = invoice.CreatedAt
        };
    }

    // 3. Faturayı Onaylama (InvoiceStatus = Confirmed, OrderStatus = Invoiced)
    public async Task ConfirmInvoiceAndOrderAsync(int invoiceId, int sellerId)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId);
        if (invoice == null || invoice.SellerId != sellerId)
            throw new KeyNotFoundException("Fatura bulunamadı.");

        if (invoice.Status != InvoiceStatus.Waiting)
            throw new InvalidOperationException("Sadece taslak (Waiting) faturalar onaylanabilir.");

        var order = await _orderRepository.GetByIdAsync(invoice.OrderId);
        if (order == null)
            throw new KeyNotFoundException("Faturaya ait sipariş bulunamadı.");

        // Durum güncellemeleri
        invoice.Status = InvoiceStatus.Confirmed;
        order.Status = OrderStatus.Invoiced;
        order.UpdatedAt = DateTime.UtcNow;

        _invoiceRepository.Update(invoice);
        _orderRepository.Update(order);

        await _unitOfWork.SaveChangesAsync();
    }

    // 4. Kargolama (OrderStatus = Shipped)
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

    // 5. Sipariş İptali (Sipariş & Fatura = Canceled, Stoklar Geri İade Edilir)
    public async Task CancelOrderAsync(int orderId, int sellerId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null || order.SellerId != sellerId)
            throw new KeyNotFoundException("Sipariş bulunamadı.");

        if (order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Delivered)
            throw new InvalidOperationException("Kargolanmış veya teslim edilmiş siparişler iptal edilemez.");

        order.Status = OrderStatus.Canceled;
        order.UpdatedAt = DateTime.UtcNow;
        _orderRepository.Update(order);

        // Varsa faturayı da iptal et
        var allInvoices = await _invoiceRepository.GetAllAsync();
        var invoice = allInvoices.FirstOrDefault(i => i.OrderId == orderId);
        if (invoice != null)
        {
            invoice.Status = InvoiceStatus.Canceled;
            _invoiceRepository.Update(invoice);
        }

        await _unitOfWork.SaveChangesAsync();
    }
    
    // Satıcının TÜM siparişlerini durum fark etmeksizin getirir
    public async Task<IEnumerable<OrderDto>> GetAllOrdersBySellerIdAsync(int sellerId)
    {
        var orders = await _orderRepository.GetAllAsync(
            o => o.Customer, 
            o => o.OrderItems
        );

        var products = await _productRepository.GetAllAsync();
        var productDict = products.ToDictionary(p => p.Id, p => p.Name);

        return orders
            .Where(o => o.SellerId == sellerId) // Status filtresi yok, hepsi geliyor!
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
                    ProductName = productDict.TryGetValue(item.ProductId, out var prodName) ? prodName : string.Empty,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    LineTotal = item.LineTotal
                }).ToList()
            });
    }

    // Satıcının kestiği tüm faturaları getirir
    public async Task<IEnumerable<InvoiceDto>> GetInvoicesBySellerIdAsync(int sellerId)
    {
        var invoices = await _invoiceRepository.GetAllAsync();

        return invoices
            .Where(i => i.SellerId == sellerId)
            .Select(i => new InvoiceDto
            {
                Id = i.Id,
                OrderId = i.OrderId,
                InvoiceNumber = i.InvoiceNumber,
                CustomerName = i.CustomerName,
                TotalAmount = i.TotalAmount,
                Status = i.Status,
                AxIntegrationStatus = i.AxIntegrationStatus,
                CreatedAt = i.CreatedAt
            });
    }
    
}