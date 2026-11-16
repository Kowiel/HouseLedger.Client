using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace HouseLedger.Client.Auth;

public sealed class JwtAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly IAuthService _authService;

    public JwtAuthenticationStateProvider(IAuthService authService)
    {
        _authService = authService;
        _authService.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        await _authService.InitializeAsync();
        var token = _authService.CurrentToken?.AccessToken;
        if (string.IsNullOrWhiteSpace(token) || !JwtReader.TryReadClaims(token, out var claims))
        {
            return AnonymousState();
        }

        var identity = new ClaimsIdentity(claims, "Bearer", ClaimTypes.Name, ClaimTypes.Role);
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    private void OnAuthenticationStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void Dispose()
    {
        _authService.AuthenticationStateChanged -= OnAuthenticationStateChanged;
    }

    private static AuthenticationState AnonymousState() =>
        new(new ClaimsPrincipal(new ClaimsIdentity()));
}