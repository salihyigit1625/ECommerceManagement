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

        // 1. ADIM: Stok Kartını Oluştur
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

        // 2. ADIM: Paylaştığın yapıya uygun olarak stock-price endpoint'inden bu ürüne ait fiyat ID'sini çek
        Guid? stockPriceId = null;
        var priceResponse = await _httpClient.GetAsync($"/api/app/stock-price?stockId={stockId}");

        if (priceResponse.IsSuccessStatusCode)
        {
            string priceJsonString = await priceResponse.Content.ReadAsStringAsync();
            var priceNode = JsonNode.Parse(priceJsonString);

            // Gelen "data" dizisinin ilk elemanının "id" değerini alıyoruz
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
        int maxResultCount = 100; // Her istekte 100 ürün çekelim
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

                    // Eğer tüm ürünler çekildiyse veya gelen ürün sayısı istenenden azsa döngüyü bitir
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
}