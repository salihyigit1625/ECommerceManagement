using AutoMapper;
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
    private readonly IGenericRepository<ProductMovement> _productMovementRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public SellerOrderService(
        IGenericRepository<Order> orderRepository,
        IGenericRepository<Invoice> invoiceRepository,
        IGenericRepository<Customer> customerRepository,
        IGenericRepository<Product> productRepository,
        IGenericRepository<ProductMovement> productMovementRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _orderRepository = orderRepository;
        _invoiceRepository = invoiceRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _productMovementRepository = productMovementRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IEnumerable<OrderDto>> GetPendingOrdersAsync(int sellerId)
    {
        var orders = await _orderRepository.GetWhereAsync(
            o => o.SellerId == sellerId && o.Status == OrderStatus.Pending,
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

    public async Task<InvoiceDto> CreateInvoiceDraftAsync(int orderId, int sellerId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, o => o.OrderItems);
        
        if (order == null || order.SellerId != sellerId)
            throw new KeyNotFoundException("Sipariş bulunamadı veya bu satıcıya ait değil.");

        if (order.Status != OrderStatus.Pending)
            throw new InvalidOperationException("Sadece 'Pending' durumundaki siparişlere fatura kesilebilir.");

        var customer = await _customerRepository.GetByIdAsync(order.CustomerId);
        string customerFullName = customer != null ? $"{customer.FirstName} {customer.LastName}" : "Bilinmeyen Müşteri";

        var invoiceItems = order.OrderItems.Select(item => new InvoiceItem
        {
            ProductId = item.ProductId,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            TaxRate = 20m,
            LineTotal = item.LineTotal
        }).ToList();

        var invoice = new Invoice
        {
            OrderId = order.Id,
            SellerId = sellerId,
            CustomerName = customerFullName,
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{order.Id}",
            TotalAmount = order.TotalAmount,
            Status = InvoiceStatus.Waiting,
            AxIntegrationStatus = AxIntegrationStatus.Pending,
            InvoiceItems = invoiceItems 
        };

        await _invoiceRepository.AddAsync(invoice);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<InvoiceDto>(invoice);
    }

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

        invoice.Status = InvoiceStatus.Confirmed;
        order.Status = OrderStatus.Invoiced;
        order.UpdatedAt = DateTime.UtcNow;

        _invoiceRepository.Update(invoice);
        _orderRepository.Update(order);

        await _unitOfWork.SaveChangesAsync();
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