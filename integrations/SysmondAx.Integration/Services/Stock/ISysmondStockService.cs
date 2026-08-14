using SysmondAx.Integration.Models.Dtos;
using SysmondAx.Integration.Models.Requests;

namespace SysmondAx.Integration.Services.Stock;

public interface ISysmondStockService
{
    Task<(Guid StockId, Guid? StockPriceId)> CreateStockAsync(SysmondStockRequest request);
    Task MapStockToWarehouseAsync(Guid stockId, Guid warehouseId, double? criticalStockLevel = null);
    Task<Guid> CreateStockReceiptAsync(Guid warehouseId, string description);
    Task AddStockReceiptItemAsync(Guid receiptId, Guid stockId, Guid warehouseId, decimal quantity, decimal unitPrice, Guid? stockPriceId);
    Task ProcessStockReceiptAsync(Guid receiptId);
    Task<List<SysmondProductDto>> GetProductsAsync();
    Task DeleteStockAsync(Guid sysmondStockId);
    Task AdjustStockQuantityAsync(Guid sysmondStockId, Guid sysmondWarehouseId, int differenceQuantity);
    Task<List<SysmondStockPriceUpdateDto>> GetAllStockPricesAsync();
    Task<SysmondStockPriceUpdateDto?> GetStockPriceAsync(Guid stockId);
    Task UpdateStockPriceAsync(SysmondStockPriceUpdateDto dto);
}