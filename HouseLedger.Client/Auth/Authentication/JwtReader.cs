using System.Security.Claims;
using System.Text.Json;

namespace HouseLedger.Client.Auth;

public static class JwtReader
{
    public static Guid? GetUserId(string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        try
        {
            using var document = ReadPayload(accessToken);
            if (document is null)
            {
                return null;
            }

            return document.RootElement.TryGetProperty("sub", out var subject)
                && Guid.TryParse(subject.GetString(), out var userId)
                ? userId
                : null;
        }
        catch (FormatException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static bool IsAccessTokenExpiring(string accessToken)
    {
        try
        {
            using var document = ReadPayload(accessToken);
            if (document is null || !document.RootElement.TryGetProperty("exp", out var expiration))
            {
                return true;
            }

            var expirationTime = DateTimeOffset.FromUnixTimeSeconds(expiration.GetInt64());
            return expirationTime <= DateTimeOffset.UtcNow.AddMinutes(1);
        }
        catch (FormatException)
        {
            return true;
        }
        catch (JsonException)
        {
            return true;
        }
    }

    public static bool TryReadClaims(string accessToken, out List<Claim> claims)
    {
        claims = [];
        try
        {
            using var document = ReadPayload(accessToken);
            if (document is null)
            {
                return false;
            }

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

    private static JsonDocument? ReadPayload(string accessToken)
    {
        var parts = accessToken.Split('.');
        if (parts.Length != 3)
        {
            return null;
        }

        var payload = parts[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        return JsonDocument.Parse(Convert.FromBase64String(payload));
    }

    private static string MapClaimType(string claimType) => claimType switch
    {
        "sub" => ClaimTypes.NameIdentifier,
        "unique_name" => ClaimTypes.Name,
        _ => claimType
    };
}
