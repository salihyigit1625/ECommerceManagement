namespace SysmondAx.Integration.Services.Auth;

public interface ISysmondAuthService
{
    Task<string> GetAccessTokenAsync();
}