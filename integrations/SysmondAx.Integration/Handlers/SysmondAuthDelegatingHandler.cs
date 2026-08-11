using System.Net.Http.Headers;
using SysmondAx.Integration.Services.Auth;

namespace SysmondAx.Integration.Handlers;

public class SysmondAuthDelegatingHandler : DelegatingHandler
{
    private readonly ISysmondAuthService _authService;

    public SysmondAuthDelegatingHandler(ISysmondAuthService authService)
    {
        _authService = authService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // 1. Auth servisinden token'ı al (Cache'den veya taze olarak)
        string token = await _authService.GetAccessTokenAsync();

        // 2. Giden HTTP isteğinin Header'ına Bearer Token olarak ekle
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 3. İsteği yola devam etmesi için bırak
        return await base.SendAsync(request, cancellationToken);
    }
}