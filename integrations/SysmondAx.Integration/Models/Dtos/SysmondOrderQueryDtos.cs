using System.Text.Json.Serialization;

namespace SysmondAx.Integration.Models.Dtos;

// --- SİPARİŞ LİSTESİ DTO'LARI ---
public class SysmondOrderQueryResponseDto
{
    [JsonPropertyName("items")]
    public List<SysmondOrderDto>? Items { get; set; }

    [JsonPropertyName("data")]
    public List<SysmondOrderDto>? Data { get; set; }
    
    public List<SysmondOrderDto> GetOrders() => Items ?? Data ?? new List<SysmondOrderDto>();
}

public class SysmondOrderDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; } 

    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    [JsonPropertyName("createdOn")]
    public DateTime CreatedOn { get; set; }
}

// --- SİPARİŞ KALEMİ (ITEM) DTO'LARI ---
public class SysmondOrderItemResponseDto
{
    [JsonPropertyName("items")]
    public List<SysmondOrderItemDto>? Items { get; set; }

    [JsonPropertyName("data")]
    public List<SysmondOrderItemDto>? Data { get; set; }

    public List<SysmondOrderItemDto> GetItems() => Items ?? Data ?? new List<SysmondOrderItemDto>();
}

public class SysmondOrderItemDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("stockId")]
    public Guid? StockId { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }
}