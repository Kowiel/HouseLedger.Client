using System.Net;
using HouseLedger.Client.Auth;
using HouseLedger.Shared.DTO.Auth;
using HouseLedger.Shared.DTO.User;
using HouseLedger.Shared.Response;
using Microsoft.AspNetCore.Components;

namespace HouseLedger.Tests;

public class AuthMessageHandlerTests
{
    [Fact]
    public async Task RejectedRefreshForRevokedOrExpiredSession_ClearsAuthenticationAndRedirectsToLogin()
    {
        var authService = new FakeAuthService { RejectForcedRefresh = true };
        var navigation = new TestNavigationManager();
        var innerHandler = new StatusCodeHandler(HttpStatusCode.Unauthorized);
        using var handler = CreateHandler(authService, navigation, innerHandler);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.example/") };

        using var response = await client.GetAsync("api/v1/tools");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(1, innerHandler.RequestCount);
        Assert.Null(authService.CurrentToken);
        Assert.Equal("/login", navigation.LastUri);
        Assert.NotNull(authService.ClearedMessage);
    }

    [Fact]
    public async Task ForbiddenResponse_RedirectsToUnauthorizedAndPreservesAuthentication()
    {
        var authService = new FakeAuthService();
        var navigation = new TestNavigationManager();
        var innerHandler = new StatusCodeHandler(HttpStatusCode.Forbidden);
        using var handler = CreateHandler(authService, navigation, innerHandler);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.example/") };

        using var response = await client.GetAsync("api/v1/tools");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(1, innerHandler.RequestCount);
        Assert.NotNull(authService.CurrentToken);
        Assert.Null(authService.ClearedMessage);
        Assert.Equal("/unauthorized", navigation.LastUri);
    }

    [Fact]
    public async Task UnauthorizedAfterSuccessfulRefresh_ClearsAuthenticationAndRedirectsToLogin()
    {
        var authService = new FakeAuthService();
        var navigation = new TestNavigationManager();
        var innerHandler = new StatusCodeHandler(HttpStatusCode.Unauthorized);
        using var handler = CreateHandler(authService, navigation, innerHandler);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.example/") };

        using var response = await client.GetAsync("api/v1/tools");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(2, innerHandler.RequestCount);
        Assert.Null(authService.CurrentToken);
        Assert.Equal("/login", navigation.LastUri);
        Assert.NotNull(authService.ClearedMessage);
    }

    private static AuthMessageHandler CreateHandler(
        IAuthService authService,
        NavigationManager navigation,
        HttpMessageHandler innerHandler) =>
        new(authService, navigation) { InnerHandler = innerHandler };

    private sealed class StatusCodeHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                RequestMessage = request
            });
        }
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public string? LastUri { get; private set; }

        public TestNavigationManager()
        {
            Initialize("https://client/", "https://client/");
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            LastUri = ToAbsoluteUri(uri).AbsolutePath;
        }

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            LastUri = ToAbsoluteUri(uri).AbsolutePath;
        }
    }

    private sealed class FakeAuthService : IAuthService
    {
        public TokenResponse? CurrentToken { get; private set; } = new()
        {
            AccessToken = "test-access-token",
            RefreshToken = "test-refresh-token",
            SessionId = Guid.NewGuid()
        };

        public Guid? CurrentUserId => null;
        public string? ClearedMessage { get; private set; }
        public bool RejectForcedRefresh { get; init; }

        public event Action? AuthenticationStateChanged
        {
            add { }
            remove { }
        }

        public event Action? SessionExpiring
        {
            add { }
            remove { }
        }

        public string? ConsumeForcedLogoutMessage() => null;

        public Task InitializeAsync() => Task.CompletedTask;

        public Task<ServiceResponse<bool>> RegisterAsync(CreateUserRequest request) =>
            Task.FromResult(new ServiceResponse<bool>());

        public Task<ServiceResponse<TokenResponse>> LoginAsync(LoginRequest request) =>
            Task.FromResult(new ServiceResponse<TokenResponse>());

        public Task<ServiceResponse<TokenResponse>> RefreshAsync(bool force = false)
        {
            var success = !force || !RejectForcedRefresh;
            return Task.FromResult(new ServiceResponse<TokenResponse>
            {
                Data = success ? CurrentToken : null,
                Success = success
            });
        }

        public Task<ServiceResponse<TokenResponse>> ConfirmSessionActivityAsync() =>
            Task.FromResult(new ServiceResponse<TokenResponse>());

        public Task<ServiceResponse<bool>> LogoutAsync() =>
            Task.FromResult(new ServiceResponse<bool>());

        public Task ClearAuthenticationAsync(string? forcedLogoutMessage = null)
        {
            CurrentToken = null;
            ClearedMessage = forcedLogoutMessage;
            return Task.CompletedTask;
        }
    }
}
