using System.Text.Json.Serialization;

namespace SysmondAx.Integration.Models.Dtos;

public class SysmondOrderStatusUpdateDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("statusNote")]
    public string? StatusNote { get; set; }
}