using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SysmondAx.Integration.Models.Responses;
using SysmondAx.Integration.Models.Settings;

namespace SysmondAx.Integration.Services.Auth;

public class SysmondAuthService : ISysmondAuthService
{
    private readonly HttpClient _httpClient;
    private readonly SysmondAxSettings _settings;
    private readonly IMemoryCache _cache;
    private const string CacheKey = "SysmondAxAccessToken";

    public SysmondAuthService(HttpClient httpClient, IOptions<SysmondAxSettings> settings, IMemoryCache cache)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _cache = cache;
    }

    public async Task<string> GetAccessTokenAsync()
    {
        if (_cache.TryGetValue(CacheKey, out string? cachedToken) && !string.IsNullOrEmpty(cachedToken))
        {
            return cachedToken;
        }

        var requestBody = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "integration_api_grant"),
            new KeyValuePair<string, string>("partner_secret", _settings.PartnerSecret),
            new KeyValuePair<string, string>("client_id", _settings.ClientId),
            new KeyValuePair<string, string>("client_secret", _settings.ClientSecret),
            new KeyValuePair<string, string>("scope", "Sysmond")
        });

        string tokenUrl = $"{_settings.BaseUrl}/connect/token";
        
        var response = await _httpClient.PostAsync(tokenUrl, requestBody);
        
        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync();
            throw new Exception($"SysmondAX Token alınamadı. Status: {response.StatusCode}, Detay: {error}");
        }

        var tokenResponse = await response.Content.ReadFromJsonAsync<SysmondTokenResponse>();
        
        if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
        {
            throw new Exception("SysmondAX Token yanıtı boş döndü.");
        }

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromSeconds(tokenResponse.ExpiresIn - 30));

        _cache.Set(CacheKey, tokenResponse.AccessToken, cacheOptions);

        return tokenResponse.AccessToken;
    }
}