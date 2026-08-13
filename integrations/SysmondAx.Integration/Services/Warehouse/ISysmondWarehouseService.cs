using SysmondAx.Integration.Models;
using SysmondAx.Integration.Models.Dtos;
using SysmondAx.Integration.Models.Requests;

namespace SysmondAx.Integration.Services.Warehouse;

public interface ISysmondWarehouseService
{
    Task<string> CreateWarehouseAsync(SysmondWarehouseRequest request);
    Task<List<SysmondWarehouseDto>> GetWarehousesAsync();
    Task<List<SysmondWarehouseStockDto>> GetWarehouseStocksAsync();
}