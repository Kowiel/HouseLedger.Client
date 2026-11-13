using System.Security.Claims;
using System.Text;
using System.Text.Json;
using HouseLedger.Client.Auth;
using HouseLedger.Shared.DTO.Auth;
using HouseLedger.Shared.Response;

namespace HouseLedger.Tests;

public class JwtReaderTests
{
    [Fact]
    public void GetUserId_ReadsGuidSubjectWithoutCheckingExpiration()
    {
        var userId = Guid.NewGuid();
        var token = CreateToken(new { sub = userId.ToString(), exp = DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds() });

        Assert.Equal(userId, JwtReader.GetUserId(token));
        Assert.Null(JwtReader.GetUserId(CreateToken(new { sub = "not-a-guid" })));
        Assert.Null(JwtReader.GetUserId(CreateToken(new { name = "user" })));
        Assert.Null(JwtReader.GetUserId(null));
    }

    [Theory]
    [InlineData(300, false)]
    [InlineData(30, true)]
    [InlineData(-300, true)]
    public void IsAccessTokenExpiring_UsesOneMinuteRefreshWindow(int secondsUntilExpiration, bool expected)
    {
        var token = CreateToken(new { exp = DateTimeOffset.UtcNow.AddSeconds(secondsUntilExpiration).ToUnixTimeSeconds() });

        Assert.Equal(expected, JwtReader.IsAccessTokenExpiring(token));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-jwt")]
    [InlineData("header.@@.signature")]
    [InlineData("header.eA.signature")]
    public void MalformedTokens_ReturnExistingFallbacks(string token)
    {
        Assert.Null(JwtReader.GetUserId(token));
        Assert.True(JwtReader.IsAccessTokenExpiring(token));
        Assert.False(JwtReader.TryReadClaims(token, out var claims));
        Assert.Empty(claims);
    }

    [Fact]
    public void MissingExpiration_IsExpiringForRefreshButAcceptedForClaims()
    {
        var token = CreateToken(new { sub = "non-guid-subject", unique_name = "Alice", role = "admin", count = 3 });

        Assert.True(JwtReader.IsAccessTokenExpiring(token));
        Assert.True(JwtReader.TryReadClaims(token, out var claims));
        Assert.Contains(claims, claim => claim.Type == ClaimTypes.NameIdentifier && claim.Value == "non-guid-subject");
        Assert.Contains(claims, claim => claim.Type == ClaimTypes.Name && claim.Value == "Alice");
        Assert.Contains(claims, claim => claim.Type == "role" && claim.Value == "admin");
        Assert.DoesNotContain(claims, claim => claim.Type == "count");
    }

    [Fact]
    public void FractionalExpiration_PreservesDifferentRefreshAndClaimsPolicies()
    {
        var token = CreateToken(new { sub = "user", exp = 1.5 });

        Assert.True(JwtReader.IsAccessTokenExpiring(token));
        Assert.True(JwtReader.TryReadClaims(token, out _));
    }

    [Fact]
    public void NearlyExpiredToken_RequiresRefreshButStillHasValidClaims()
    {
        var token = CreateToken(new { sub = "user", exp = DateTimeOffset.UtcNow.AddSeconds(30).ToUnixTimeSeconds() });

        Assert.True(JwtReader.IsAccessTokenExpiring(token));
        Assert.True(JwtReader.TryReadClaims(token, out _));
    }

    [Fact]
    public void InvalidClaimTypes_PreserveExistingExceptions()
    {
        var invalidSubject = CreateToken(new { sub = 123 });
        var invalidExpiration = CreateToken(new { sub = "user", exp = "invalid" });

        Assert.Throws<InvalidOperationException>(() => JwtReader.GetUserId(invalidSubject));
        Assert.Throws<InvalidOperationException>(() => JwtReader.IsAccessTokenExpiring(invalidExpiration));
        Assert.Throws<InvalidOperationException>(() => JwtReader.TryReadClaims(invalidExpiration, out _));
        Assert.Throws<NullReferenceException>(() => JwtReader.IsAccessTokenExpiring(null!));
    }

    [Fact]
    public void Claims_RejectExpiredTokenAndMissingSubject()
    {
        var expired = CreateToken(new { sub = "user", exp = DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds() });
        var missingSubject = CreateToken(new { unique_name = "Alice", exp = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds() });

        Assert.False(JwtReader.TryReadClaims(expired, out _));
        Assert.False(JwtReader.TryReadClaims(missingSubject, out _));
    }

    [Fact]
    public async Task AuthenticationStateProvider_UsesReaderClaimsAndExpiry()
    {
        var userId = Guid.NewGuid().ToString();
        var authService = new FakeAuthService(CreateToken(new
        {
            sub = userId,
            unique_name = "Alice",
            exp = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds()
        }));
        using var provider = new JwtAuthenticationStateProvider(authService);

        var authenticated = await provider.GetAuthenticationStateAsync();
        Assert.True(authenticated.User.Identity?.IsAuthenticated);
        Assert.Equal("Alice", authenticated.User.Identity?.Name);
        Assert.Equal(userId, authenticated.User.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal(1, authService.InitializationCount);

        authService.Token = CreateToken(new { sub = userId, exp = DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds() });
        var expired = await provider.GetAuthenticationStateAsync();
        Assert.False(expired.User.Identity?.IsAuthenticated);
    }

    private static string CreateToken(object payload)
    {
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        return $"header.{encoded}.signature";
    }

    private sealed class FakeAuthService(string token) : IAuthService
    {
        public string Token { get; set; } = token;
        public int InitializationCount { get; private set; }
        public TokenResponse? CurrentToken => new() { AccessToken = Token };
        public Guid? CurrentUserId => null;
        public event Action? AuthenticationStateChanged { add { } remove { } }
        public Task InitializeAsync()
        {
            InitializationCount++;
            return Task.CompletedTask;
        }
        public Task<ServiceResponse<TokenResponse>> LoginAsync(LoginRequest request) => throw new NotSupportedException();
        public Task<ServiceResponse<TokenResponse>> RefreshAsync(bool force = false, bool isActivityConfirmation = false) => throw new NotSupportedException();
        public Task<ServiceResponse<bool>> LogoutAsync() => throw new NotSupportedException();
        public Task ClearAuthenticationAsync(string? forcedLogoutMessage = null) => throw new NotSupportedException();
    }
}
