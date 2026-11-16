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
        // Arrange: token contains a GUID subject but is already expired.
        var userId = Guid.NewGuid();
        var token = CreateToken(new { sub = userId.ToString(), exp = DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds() });

        // Assert: subject parsing is independent of expiration and malformed subjects are rejected.
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
        // Arrange: token expiration varies relative to the one-minute refresh window.
        var token = CreateToken(new { exp = DateTimeOffset.UtcNow.AddSeconds(secondsUntilExpiration).ToUnixTimeSeconds() });

        // Assert: expiring logic tracks the configured refresh threshold.
        Assert.Equal(expected, JwtReader.IsAccessTokenExpiring(token));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-jwt")]
    [InlineData("header.@@.signature")]
    [InlineData("header.eA.signature")]
    public void MalformedTokens_ReturnExistingFallbacks(string token)
    {
        // Assert: malformed tokens trigger safe fallbacks across all reader APIs.
        Assert.Null(JwtReader.GetUserId(token));
        Assert.True(JwtReader.IsAccessTokenExpiring(token));
        Assert.False(JwtReader.TryReadClaims(token, out var claims));
        Assert.Empty(claims);
    }

    [Fact]
    public void MissingExpiration_IsExpiringForRefreshButAcceptedForClaims()
    {
        // Arrange: token has claims but no expiration field.
        var token = CreateToken(new { sub = "non-guid-subject", unique_name = "Alice", role = "admin", count = 3 });

        // Assert: refresh check treats it as expiring while claim extraction still succeeds.
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
        // Arrange: expiration value is a non-integer number.
        var token = CreateToken(new { sub = "user", exp = 1.5 });

        // Assert: token is considered expiring while claims can still be read.
        Assert.True(JwtReader.IsAccessTokenExpiring(token));
        Assert.True(JwtReader.TryReadClaims(token, out _));
    }

    [Fact]
    public void NearlyExpiredToken_RequiresRefreshButStillHasValidClaims()
    {
        // Arrange: token expires inside the refresh window.
        var token = CreateToken(new { sub = "user", exp = DateTimeOffset.UtcNow.AddSeconds(30).ToUnixTimeSeconds() });

        // Assert: refresh is required but token claims remain valid.
        Assert.True(JwtReader.IsAccessTokenExpiring(token));
        Assert.True(JwtReader.TryReadClaims(token, out _));
    }

    [Fact]
    public void InvalidClaimTypes_PreserveExistingExceptions()
    {
        // Arrange: token contains claim types that violate parser expectations.
        var invalidSubject = CreateToken(new { sub = 123 });
        var invalidExpiration = CreateToken(new { sub = "user", exp = "invalid" });

        // Assert: reader preserves current exception behavior for invalid claim types.
        Assert.Throws<InvalidOperationException>(() => JwtReader.GetUserId(invalidSubject));
        Assert.Throws<InvalidOperationException>(() => JwtReader.IsAccessTokenExpiring(invalidExpiration));
        Assert.Throws<InvalidOperationException>(() => JwtReader.TryReadClaims(invalidExpiration, out _));
        Assert.Throws<NullReferenceException>(() => JwtReader.IsAccessTokenExpiring(null!));
    }

    [Fact]
    public void Claims_RejectExpiredTokenAndMissingSubject()
    {
        // Arrange: one token is expired and one omits the subject claim.
        var expired = CreateToken(new { sub = "user", exp = DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds() });
        var missingSubject = CreateToken(new { unique_name = "Alice", exp = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds() });

        // Assert: claim extraction rejects both tokens.
        Assert.False(JwtReader.TryReadClaims(expired, out _));
        Assert.False(JwtReader.TryReadClaims(missingSubject, out _));
    }

    [Fact]
    public async Task AuthenticationStateProvider_UsesReaderClaimsAndExpiry()
    {
        // Arrange: authenticated token with valid claims and expiration.
        var userId = Guid.NewGuid().ToString();
        var authService = new FakeAuthService(CreateToken(new
        {
            sub = userId,
            unique_name = "Alice",
            exp = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds()
        }));
        using var provider = new JwtAuthenticationStateProvider(authService);

        // Act: read authenticated state, then simulate expiration.
        var authenticated = await provider.GetAuthenticationStateAsync();
        Assert.True(authenticated.User.Identity?.IsAuthenticated);
        Assert.Equal("Alice", authenticated.User.Identity?.Name);
        Assert.Equal(userId, authenticated.User.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal(1, authService.InitializationCount);

        authService.Token = CreateToken(new { sub = userId, exp = DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds() });
        var expired = await provider.GetAuthenticationStateAsync();

        // Assert: expired token results in anonymous principal.
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
