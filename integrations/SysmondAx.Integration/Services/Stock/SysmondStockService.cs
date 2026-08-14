using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using SysmondAx.Integration.Models;
using SysmondAx.Integration.Models.Dtos;
using SysmondAx.Integration.Models.Requests;

namespace SysmondAx.Integration.Services.Stock;

public class SysmondStockService : ISysmondStockService
{
    private readonly HttpClient _httpClient;
    private readonly Guid _sysmondCompanyId = Guid.Parse("f9e4c15a-307a-d6e5-495a-3a22008d01a1");

    public SysmondStockService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<(Guid StockId, Guid? StockPriceId)> CreateStockAsync(SysmondStockRequest request)
    {
        string code = string.IsNullOrWhiteSpace(request.Code) 
            ? $"STK-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}" 
            : request.Code;

        var sysmondDto = new SysmondStockCreateDto
        {
            CompanyId = _sysmondCompanyId,
            Name = request.Name,
            Description = request.Description,
            Code = code,
            VatPercent = request.VatPercent ?? 0m,
            Price = new SysmondStockPriceDto
            {
                SaleUnitPrice = request.SalePrice,
                PurchaseUnitPrice = request.PurchasePrice
            }
        };

        if (request.WarehouseSysmondId.HasValue && request.InitialQuantity.HasValue)
        {
            sysmondDto.OpeningQuantity = new List<SysmondStockOpeningQuantityDto>
            {
                new SysmondStockOpeningQuantityDto
                {
                    WarehouseId = request.WarehouseSysmondId.Value,
                    Quantity = request.InitialQuantity.Value
                }
            };
        }

        var response = await _httpClient.PostAsJsonAsync("/api/app/stock", sysmondDto);

        if (!response.IsSuccessStatusCode)
        {
            string errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Sysmond Stok oluşturulamadı. Hata: {errorContent}");
        }

        string jsonString = await response.Content.ReadAsStringAsync();
        var jsonNode = JsonNode.Parse(jsonString);
        
        string stockIdStr = jsonNode?["data"]?["id"]?.ToString() ?? string.Empty;
        if (!Guid.TryParse(stockIdStr, out Guid stockId))
        {
            throw new Exception("Sysmond'dan geçerli bir Stock ID alınamadı.");
        }

        Guid? stockPriceId = null;
        var priceResponse = await _httpClient.GetAsync($"/api/app/stock-price?stockId={stockId}");

        if (priceResponse.IsSuccessStatusCode)
        {
            string priceJsonString = await priceResponse.Content.ReadAsStringAsync();
            var priceNode = JsonNode.Parse(priceJsonString);

            string? priceIdStr = priceNode?["data"]?[0]?["id"]?.ToString();

            if (!string.IsNullOrEmpty(priceIdStr) && Guid.TryParse(priceIdStr, out var parsedPriceId))
            {
                stockPriceId = parsedPriceId;
            }
        }

        return (stockId, stockPriceId);
    }
    
    public async Task MapStockToWarehouseAsync(Guid stockId, Guid warehouseId, double? criticalStockLevel = null)
    {
        var sysmondDto = new SysmondWarehouseStockCreateDto
        {
            StockId = stockId,
            WarehouseId = warehouseId,
            CriticalStockLevel = criticalStockLevel
        };

        var response = await _httpClient.PostAsJsonAsync("/api/app/warehouse-stock", sysmondDto);

        if (!response.IsSuccessStatusCode)
        {
            string errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Sysmond Stok-Depo eşleştirmesi başarısız oldu. Hata: {errorContent}");
        }
    }
    
    public async Task<Guid> CreateStockReceiptAsync(Guid warehouseId, string description)
    {
        var dto = new SysmondStockReceiptCreateDto
        {
            WarehouseId = warehouseId,
            Description = description,
            Type = 10,
            TransactionDate = DateTime.UtcNow
        };

        var response = await _httpClient.PostAsJsonAsync("/api/app/stock-receipt/stock-receipt", dto);

        if (response.IsSuccessStatusCode)
        {
            string jsonString = await response.Content.ReadAsStringAsync();
            var jsonNode = JsonNode.Parse(jsonString);
            string idStr = jsonNode?["data"]?["id"]?.ToString() ?? string.Empty;
        
            if (Guid.TryParse(idStr, out Guid receiptId))
                return receiptId;
        }

        string errorContent = await response.Content.ReadAsStringAsync();
        throw new Exception($"Sysmond Stok Fişi oluşturulamadı. Hata: {errorContent}");
    }

    public async Task AddStockReceiptItemAsync(Guid receiptId, Guid stockId, Guid warehouseId, decimal quantity, decimal unitPrice, Guid? stockPriceId)
    {
        var dto = new SysmondStockReceiptItemCreateDto
        {
            StockReceiptId = receiptId,
            StockId = stockId,
            WarehouseId = warehouseId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            StockPriceId = stockPriceId,
            CurrencyExchangeRate = 1
        };

        var response = await _httpClient.PostAsJsonAsync("/api/app/stock-receipt/stock-receipt-item", dto);

        if (!response.IsSuccessStatusCode)
        {
            string errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Sysmond Fişe Kalem eklenemedi. Hata: {errorContent}");
        }
    }
    
    public async Task ProcessStockReceiptAsync(Guid receiptId)
    {
        var response = await _httpClient.PostAsync($"/api/app/stock-receipt/{receiptId}/process-stock-receipt", null);

        if (!response.IsSuccessStatusCode)
        {
            string errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Sysmond Stok Fişi onaylanamadı (Process). Hata: {errorContent}");
        }
    }
    
    public async Task<List<SysmondProductDto>> GetProductsAsync()
    {
        var allProducts = new List<SysmondProductDto>();
        int skipCount = 0;
        int maxResultCount = 100; 
        bool hasMore = true;

        while (hasMore)
        {
            var url = $"/api/app/stock-query?CompanyId={_sysmondCompanyId}&SkipCount={skipCount}&MaxResultCount={maxResultCount}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Sysmond ürünleri getirilemedi. Hata: {errorContent}");
            }

            string jsonString = await response.Content.ReadAsStringAsync();
            var jsonNode = JsonNode.Parse(jsonString);

            var itemsNode = jsonNode?["items"];
            int totalCount = jsonNode?["totalCount"]?.GetValue<int>() ?? 0;

            if (itemsNode != null)
            {
                var products = itemsNode.Deserialize<List<SysmondProductDto>>();
                if (products != null && products.Any())
                {
                    allProducts.AddRange(products);
                    skipCount += maxResultCount;

                    if (allProducts.Count >= totalCount || products.Count < maxResultCount)
                    {
                        hasMore = false;
                    }
                }
                else
                {
                    hasMore = false;
                }
            }
            else
            {
                hasMore = false;
            }
        }

        return allProducts;
    }
    
    
    public async Task DeleteStockAsync(Guid sysmondStockId)
    {
        var response = await _httpClient.DeleteAsync($"/api/app/stock/{sysmondStockId}");

        if (!response.IsSuccessStatusCode)
        {
            string errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Sysmond üzerinden stok silinemedi (ID: {sysmondStockId}). Hata: {errorContent}");
        }
    }
    
    public async Task AdjustStockQuantityAsync(Guid sysmondStockId, Guid sysmondWarehouseId, int differenceQuantity)
    {
        int receiptType = differenceQuantity > 0 ? 10 : 20;
        decimal absoluteQuantity = Math.Abs(differenceQuantity);

        var receiptHeaderDto = new SysmondStockReceiptCreateDto
        {
            Type = receiptType,
            WarehouseId = sysmondWarehouseId
        };

        var headerResponse = await _httpClient.PostAsJsonAsync("/api/app/stock-receipt/stock-receipt", receiptHeaderDto);
        if (!headerResponse.IsSuccessStatusCode)
        {
            string errorContent = await headerResponse.Content.ReadAsStringAsync();
            throw new Exception($"Stok fişi (başlık) oluşturulamadı. Detay: {errorContent}");
        }

        string headerJsonString = await headerResponse.Content.ReadAsStringAsync();
        var headerNode = System.Text.Json.Nodes.JsonNode.Parse(headerJsonString);
        
        string? receiptIdStr = headerNode?["id"]?.ToString() ?? headerNode?["data"]?["id"]?.ToString();
        if (string.IsNullOrEmpty(receiptIdStr) || !Guid.TryParse(receiptIdStr, out Guid receiptId))
        {
            throw new Exception("Sysmond'dan geçerli bir Fiş (Receipt) ID alınamadı.");
        }

        Guid? stockPriceId = null;
        var priceResponse = await _httpClient.GetAsync($"/api/app/stock-price?stockId={sysmondStockId}");

        if (priceResponse.IsSuccessStatusCode)
        {
            string priceJsonString = await priceResponse.Content.ReadAsStringAsync();
            var priceNode = System.Text.Json.Nodes.JsonNode.Parse(priceJsonString);

            string? priceIdStr = priceNode?["data"]?[0]?["id"]?.ToString();

            if (!string.IsNullOrEmpty(priceIdStr) && Guid.TryParse(priceIdStr, out var parsedPriceId))
            {
                stockPriceId = parsedPriceId;
            }
        }

        var receiptItemDto = new SysmondStockReceiptItemCreateDto()
        {
            StockReceiptId = receiptId,
            StockId = sysmondStockId,
            WarehouseId = sysmondWarehouseId,
            Quantity = absoluteQuantity,
            UnitPrice = 1,
            StockPriceId = stockPriceId,
            CurrencyExchangeRate = 1
        };

        var itemResponse = await _httpClient.PostAsJsonAsync("/api/app/stock-receipt/stock-receipt-item", receiptItemDto);
        if (!itemResponse.IsSuccessStatusCode)
        {
            string errorContent = await itemResponse.Content.ReadAsStringAsync();
            throw new Exception($"Stok fişine kalem eklenemedi. Detay: {errorContent}");
        }

        var processResponse = await _httpClient.PostAsync($"/api/app/stock-receipt/{receiptId}/process-stock-receipt", null);
        if (!processResponse.IsSuccessStatusCode)
        {
            string errorContent = await processResponse.Content.ReadAsStringAsync();
            throw new Exception($"Stok fişi onaylanamadı (Process). Detay: {errorContent}");
        }
    }
    
    public async Task<List<SysmondStockPriceUpdateDto>> GetAllStockPricesAsync()
    {
        var allPrices = new List<SysmondStockPriceUpdateDto>();
        int skipCount = 0;
        int maxResultCount = 100;
        bool hasMore = true;

        while (hasMore)
        {
            var url = $"/api/app/stock-price?CompanyId={_sysmondCompanyId}&SkipCount={skipCount}&MaxResultCount={maxResultCount}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Sysmond fiyat listesi getirilemedi. Hata: {errorContent}");
            }

            string jsonString = await response.Content.ReadAsStringAsync();
            var jsonNode = System.Text.Json.Nodes.JsonNode.Parse(jsonString);

            var itemsNode = jsonNode?["data"] ?? jsonNode?["items"];
            int totalCount = jsonNode?["totalCount"]?.GetValue<int>() ?? 0;

            if (itemsNode != null)
            {
                var prices = itemsNode.Deserialize<List<SysmondStockPriceUpdateDto>>();
                if (prices != null && prices.Any())
                {
                    allPrices.AddRange(prices);
                    skipCount += maxResultCount;

                    if (allPrices.Count >= totalCount || prices.Count < maxResultCount)
                    {
                        hasMore = false;
                    }
                }
                else
                {
                    hasMore = false;
                }
            }
            else
            {
                hasMore = false;
            }
        }

        return allPrices;
    }

    public async Task<SysmondStockPriceUpdateDto?> GetStockPriceAsync(Guid stockId)
    {
        var response = await _httpClient.GetAsync($"/api/app/stock-price?stockId={stockId}");
        if (response.IsSuccessStatusCode)
        {
            string jsonString = await response.Content.ReadAsStringAsync();
            var jsonNode = System.Text.Json.Nodes.JsonNode.Parse(jsonString);
            var itemsNode = jsonNode?["items"] ?? jsonNode?["data"];
        
            if (itemsNode is System.Text.Json.Nodes.JsonArray array && array.Count > 0)
            {
                return array[0].Deserialize<SysmondStockPriceUpdateDto>();
            }
        }
        return null;
    }
    
    public async Task UpdateStockPriceAsync(SysmondStockPriceUpdateDto dto)
    {
        var response = await _httpClient.PutAsJsonAsync("/api/app/stock-price", dto);
        if (!response.IsSuccessStatusCode)
        {
            string errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Sysmond fiyat güncellemesi başarısız oldu. Detay: {errorContent}");
        }
    }
    
    
    
    
}