namespace SysmondAx.Integration.Services.Stock;

public interface ISysmondStockService
{
    // E-ticaret sistemindeki ürünü Sysmond'a gönderir ve Sysmond'un oluşturduğu ID'yi döner
    Task<string> CreateStockCardAsync(object productDto); // object kısmını kendi Product modelinle değiştirebilirsin
}