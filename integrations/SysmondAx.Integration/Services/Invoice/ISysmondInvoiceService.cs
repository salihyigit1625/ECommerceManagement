using SysmondAx.Integration.Models.Dtos;

namespace SysmondAx.Integration.Services.Invoice;

public interface ISysmondInvoiceService
{
    Task<Guid> CreateDraftInvoiceAsync(SysmondInvoiceDraftCreateDto dto);
    Task AddInvoiceItemAsync(SysmondInvoiceItemCreateDto dto);
}