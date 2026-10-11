using System.Security.Claims;
using System.Text.Json;
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

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = _authService.CurrentToken?.AccessToken;
        if (string.IsNullOrWhiteSpace(token) || !TryReadClaims(token, out var claims))
        {
            return Task.FromResult(AnonymousState());
        }

        var identity = new ClaimsIdentity(claims, "Bearer", ClaimTypes.Name, ClaimTypes.Role);
        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
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

    private static bool TryReadClaims(string accessToken, out List<Claim> claims)
    {
        claims = [];
        var parts = accessToken.Split('.');
        if (parts.Length != 3)
        {
            return false;
        }

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            using var document = JsonDocument.Parse(Convert.FromBase64String(payload));

            if (document.RootElement.TryGetProperty("exp", out var expiration) &&
                expiration.TryGetInt64(out var expirationValue) &&
                DateTimeOffset.FromUnixTimeSeconds(expirationValue) <= DateTimeOffset.UtcNow)
            {
                return false;
            }

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.String)
                {
                    claims.Add(new Claim(MapClaimType(property.Name), property.Value.GetString() ?? string.Empty));
                }
            }

            return claims.Any(claim => claim.Type == ClaimTypes.NameIdentifier);
        }
        catch (FormatException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string MapClaimType(string claimType) => claimType switch
    {
        "sub" => ClaimTypes.NameIdentifier,
        "unique_name" => ClaimTypes.Name,
        _ => claimType
    };
}