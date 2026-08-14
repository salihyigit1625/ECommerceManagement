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
        string token = await _authService.GetAccessTokenAsync();

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }
}