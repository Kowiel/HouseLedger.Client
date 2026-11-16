using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using HouseLedger.Client.Auth;
using HouseLedger.Shared.DTO.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace HouseLedger.Tests;

public class AuthServiceNotificationTests
{
    [Fact]
    public async Task RepeatedClear_NotifiesProviderOnceAndPreservesForcedMessage()
    {
        // Arrange: authenticated session with provider subscription.
        await using var fixture = new AuthFixture();
        using var provider = new JwtAuthenticationStateProvider(fixture.Auth);
        var notifications = new List<Task<AuthenticationState>>();
        provider.AuthenticationStateChanged += notifications.Add;
        Assert.True((await provider.GetAuthenticationStateAsync()).User.Identity?.IsAuthenticated);

        // Act: clear authentication twice with different messages.
        await fixture.Auth.ClearAuthenticationAsync("First message");
        await fixture.Auth.ClearAuthenticationAsync("Second message");

        // Assert: provider notifies once and keeps the first forced message.
        var state = await Assert.Single(notifications);
        Assert.False(state.User.Identity?.IsAuthenticated);
        Assert.Null(fixture.Auth.CurrentToken);
        Assert.Null(fixture.Js.StoredAuthentication);
        Assert.Equal("First message", fixture.Session.ConsumeForcedLogoutMessage());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EmptyLoginResponse_NotifiesOnlyWhenClearingExistingSession(bool initiallyAuthenticated)
    {
        // Arrange: login endpoint returns no token.
        await using var fixture = new AuthFixture(initiallyAuthenticated);
        fixture.Handler.LoginToken = null;
        await fixture.Auth.InitializeAsync();
        var notifications = 0;
        fixture.Auth.AuthenticationStateChanged += () => notifications++;

        // Act: attempt login.
        var result = await fixture.Auth.LoginAsync(new LoginRequest());

        // Assert: login fails and only an existing session triggers one state change.
        Assert.False(result.Success);
        Assert.Null(fixture.Auth.CurrentToken);
        Assert.Null(fixture.Js.StoredAuthentication);
        Assert.Equal(initiallyAuthenticated ? 1 : 0, notifications);
        Assert.Equal(1, fixture.Handler.RequestedPaths.Count(path => path == "/api/v1/auth/login"));
    }

    [Fact]
    public async Task EmptyRefreshResponse_NotifiesOnceAndClearsSession()
    {
        // Arrange: refresh endpoint responds with an empty payload.
        await using var fixture = new AuthFixture();
        fixture.Handler.RefreshToken = null;
        await fixture.Auth.InitializeAsync();
        var notifications = 0;
        fixture.Auth.AuthenticationStateChanged += () => notifications++;

        // Act: force token refresh.
        var result = await fixture.Auth.RefreshAsync(force: true);

        // Assert: refresh failure clears session and emits one change.
        Assert.False(result.Success);
        Assert.Null(fixture.Auth.CurrentToken);
        Assert.Null(fixture.Js.StoredAuthentication);
        Assert.Equal(1, notifications);
        Assert.Equal(1, fixture.Handler.RequestedPaths.Count(path => path == "/api/v1/auth/refresh"));
        Assert.Equal("Your session expired or could not be renewed. Please sign in again.", fixture.Session.ConsumeForcedLogoutMessage());
    }

    [Fact]
    public async Task FailedRefresh_FollowedByAnotherClear_NotifiesOnce()
    {
        // Arrange: refresh endpoint rejects the forced refresh request.
        await using var fixture = new AuthFixture();
        fixture.Handler.RefreshStatus = HttpStatusCode.Unauthorized;
        await fixture.Auth.InitializeAsync();
        var notifications = 0;
        fixture.Auth.AuthenticationStateChanged += () => notifications++;

        // Act: refresh fails, then clear is called again.
        var result = await fixture.Auth.RefreshAsync(force: true);
        await fixture.Auth.ClearAuthenticationAsync("Another message");

        // Assert: only one authentication change is raised.
        Assert.False(result.Success);
        Assert.Equal(1, notifications);
        Assert.Null(fixture.Auth.CurrentToken);
        Assert.Equal(1, fixture.Handler.RequestedPaths.Count(path => path == "/api/v1/auth/refresh"));
        Assert.Equal("Your session expired or was revoked. Please sign in again.", fixture.Session.ConsumeForcedLogoutMessage());
    }

    [Fact]
    public async Task SuccessfulLoginAndRefresh_EachNotifyEvenForSameToken()
    {
        // Arrange: start unauthenticated so both login and refresh transition state.
        await using var fixture = new AuthFixture(initiallyAuthenticated: false);
        await fixture.Auth.InitializeAsync();
        var notifications = 0;
        fixture.Auth.AuthenticationStateChanged += () => notifications++;

        // Act: login then force refresh.
        var login = await fixture.Auth.LoginAsync(new LoginRequest());
        var refresh = await fixture.Auth.RefreshAsync(force: true);

        // Assert: each operation notifies and both endpoints are called once.
        Assert.True(login.Success);
        Assert.True(refresh.Success);
        Assert.Equal(2, notifications);
        Assert.Equal(1, fixture.Handler.RequestedPaths.Count(path => path == "/api/v1/auth/login"));
        Assert.Equal(1, fixture.Handler.RequestedPaths.Count(path => path == "/api/v1/auth/refresh"));
        Assert.Equal(fixture.Token.AccessToken, fixture.Auth.CurrentToken?.AccessToken);
    }

    [Fact]
    public async Task ExistingValidTokenWithoutRefresh_DoesNotNotify()
    {
        // Arrange: service initializes with an already valid token.
        await using var fixture = new AuthFixture();
        await fixture.Auth.InitializeAsync();
        var notifications = 0;
        fixture.Auth.AuthenticationStateChanged += () => notifications++;

        // Act: attempt non-forced refresh.
        var result = await fixture.Auth.RefreshAsync();

        // Assert: token is reused without network refresh or notifications.
        Assert.True(result.Success);
        Assert.Equal(0, notifications);
        Assert.DoesNotContain("/api/v1/auth/refresh", fixture.Handler.RequestedPaths);
    }

    [Fact]
    public async Task AlreadyLoggedOut_DoesNotNotify()
    {
        // Arrange: start with no authenticated session.
        await using var fixture = new AuthFixture(initiallyAuthenticated: false);
        await fixture.Auth.InitializeAsync();
        var notifications = 0;
        fixture.Auth.AuthenticationStateChanged += () => notifications++;

        // Act: call logout.
        var result = await fixture.Auth.LogoutAsync();

        // Assert: no state change and no logout call because already logged out.
        Assert.True(result.Success);
        Assert.Equal(0, notifications);
        Assert.DoesNotContain("/api/v1/auth/logout", fixture.Handler.RequestedPaths);
    }

    [Fact]
    public async Task CrossTabOwnershipChange_NotifiesOnceAndDoesNotRemoveOtherTabSession()
    {
        // Arrange: another tab takes ownership of persisted authentication.
        await using var fixture = new AuthFixture();
        await fixture.Auth.InitializeAsync();
        var notifications = 0;
        fixture.Auth.AuthenticationStateChanged += () => notifications++;
        var otherTab = JsonSerializer.Serialize(new { TabId = "other-tab", Token = fixture.Token });
        fixture.Js.StoredAuthentication = otherTab;

        // Act: process duplicate storage changes and then clear locally.
        await fixture.Storage.OnAuthStorageChanged(otherTab);
        await fixture.Storage.OnAuthStorageChanged(otherTab);
        await fixture.Auth.ClearAuthenticationAsync();

        // Assert: only one notification and the other tab's value remains in storage.
        Assert.Equal(1, notifications);
        Assert.Null(fixture.Auth.CurrentToken);
        Assert.Equal(otherTab, fixture.Js.StoredAuthentication);
    }

    private sealed class AuthFixture : IAsyncDisposable
    {
        private readonly HttpClient _client;

        public TokenResponse Token { get; }
        public FakeJsRuntime Js { get; }
        public TestHttpHandler Handler { get; }
        public AuthStorageService Storage { get; }
        public InactivityService Inactivity { get; }
        public SessionService Session { get; }
        public AuthService Auth { get; }

        public AuthFixture(bool initiallyAuthenticated = true)
        {
            var payload = JsonSerializer.Serialize(new
            {
                sub = Guid.NewGuid().ToString(),
                exp = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds()
            });
            var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            Token = new TokenResponse
            {
                AccessToken = $"header.{encoded}.signature",
                RefreshToken = "refresh-token",
                SessionId = Guid.NewGuid()
            };
            Js = new FakeJsRuntime
            {
                StoredAuthentication = initiallyAuthenticated
                    ? JsonSerializer.Serialize(new { TabId = "test-tab", Token })
                    : null
            };
            Handler = new TestHttpHandler { LoginToken = Token, RefreshToken = Token };
            _client = new HttpClient(Handler) { BaseAddress = new Uri("https://api.example/") };
            Storage = new AuthStorageService(Js);
            Inactivity = new InactivityService(Js, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(2));
            var forcedLogoutMessageStore = new ForcedLogoutMessageStore();
            Auth = new AuthService(_client, Storage, Inactivity, forcedLogoutMessageStore);
            Session = new SessionService(Inactivity, new TestNavigationManager(), Auth, forcedLogoutMessageStore);
        }

        public async ValueTask DisposeAsync()
        {
            await Session.DisposeAsync();
            await Storage.DisposeAsync();
            await Inactivity.DisposeAsync();
            _client.Dispose();
        }
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("https://client/", "https://client/");
        protected override void NavigateToCore(string uri, bool forceLoad) { }
    }

    private sealed class TestHttpHandler : HttpMessageHandler
    {
        public TokenResponse? LoginToken { get; set; }
        public TokenResponse? RefreshToken { get; set; }
        public HttpStatusCode RefreshStatus { get; set; } = HttpStatusCode.OK;
        public List<string> RequestedPaths { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            RequestedPaths.Add(path);
            return Task.FromResult(path switch
            {
                "/api/v1/auth/login" => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(LoginToken) },
                "/api/v1/auth/refresh" when RefreshStatus == HttpStatusCode.OK =>
                    new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(RefreshToken) },
                "/api/v1/auth/refresh" => new HttpResponseMessage(RefreshStatus) { Content = new StringContent("refresh failed") },
                _ => new HttpResponseMessage(HttpStatusCode.OK)
            });
        }
    }

    private sealed class FakeJsRuntime : IJSRuntime
    {
        public string? StoredAuthentication { get; set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            object? result = identifier switch
            {
                "houseLedgerAuthStorage.getTabId" => "test-tab",
                "houseLedgerAuthStorage.subscribe" => 1,
                "houseLedgerAuthStorage.get" => StoredAuthentication,
                _ => null
            };
            if (identifier == "houseLedgerAuthStorage.set")
            {
                StoredAuthentication = (string?)args![1];
            }
            else if (identifier == "houseLedgerAuthStorage.remove")
            {
                StoredAuthentication = null;
            }

            return ValueTask.FromResult(result is null ? default! : (TValue)result);
        }
    }
}
