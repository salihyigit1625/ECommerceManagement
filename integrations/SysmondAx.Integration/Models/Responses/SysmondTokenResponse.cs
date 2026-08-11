using System.Text.Json.Serialization;

namespace SysmondAx.Integration.Models.Responses;

public class SysmondTokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}