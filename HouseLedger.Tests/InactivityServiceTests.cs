using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HouseLedger.Client.Auth;
using HouseLedger.Shared.DTO.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace HouseLedger.Tests;

public class InactivityServiceTests
{
    [Fact]
    public async Task Monitor_StartsOnceWithConfiguredTimings_AndStopsOnDisposal()
    {
        var js = new FakeJsRuntime();
        var service = new InactivityService(js, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(2));

        await service.SetActiveAsync(true);
        await service.SetActiveAsync(true);
        var start = Assert.Single(js.Calls, call => call.Identifier == "houseLedgerInactivity.startInactivityMonitor");
        Assert.Equal(600000d, start.Arguments[0]);
        Assert.Equal(120000d, start.Arguments[1]);
        Assert.Same(service, Assert.IsType<DotNetObjectReference<InactivityService>>(start.Arguments[2]).Value);

        await service.ConfirmActivityAsync();
        await service.SetActiveAsync(false);
        await service.SetActiveAsync(false);
        await service.SetActiveAsync(true);
        await service.DisposeAsync();

        Assert.Single(js.Calls, call => call.Identifier == "houseLedgerInactivity.confirmActivity");
        Assert.Equal(2, js.Calls.Count(call => call.Identifier == "houseLedgerInactivity.startInactivityMonitor"));
        Assert.Equal(2, js.Calls.Count(call => call.Identifier == "houseLedgerInactivity.stopInactivityMonitor"));
    }

    [Fact]
    public async Task Callbacks_RaiseWarning_AndAwaitTimeoutHandlers()
    {
        var js = new FakeJsRuntime();
        await using var service = new InactivityService(js, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(2));
        var warnings = 0;
        var timeoutCompleted = false;
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        service.SessionExpiring += () => warnings++;
        service.InactivityTimedOut += async () =>
        {
            await completion.Task;
            timeoutCompleted = true;
        };

        await service.OnSessionExpiringAsync();
        var timeout = service.OnInactivityTimeoutAsync();
        Assert.False(timeout.IsCompleted);
        completion.SetResult();
        await timeout;

        Assert.Equal(1, warnings);
        Assert.True(timeoutCompleted);
    }

    [Fact]
    public async Task SessionService_ForwardsWarning_AndLogsOutOnInactivity()
    {
        await using var fixture = new AuthFixture();
        var warnings = 0;
        var authenticationChanges = 0;
        fixture.Session.SessionExpiring += () => warnings++;
        fixture.Auth.AuthenticationStateChanged += () => authenticationChanges++;

        await fixture.Auth.InitializeAsync();
        await fixture.Inactivity.OnSessionExpiringAsync();
        await fixture.Inactivity.OnInactivityTimeoutAsync();

        Assert.Equal(1, warnings);
        Assert.Equal(1, authenticationChanges);
        Assert.Null(fixture.Auth.CurrentToken);
        Assert.Equal("You were logged out due to inactivity. Please sign in again.", fixture.Session.ConsumeForcedLogoutMessage());
        Assert.Equal("/login", fixture.Navigation.LastUri);
        Assert.Contains("/api/v1/auth/logout", fixture.Handler.RequestedPaths);
        Assert.Single(fixture.Js.Calls, call => call.Identifier == "houseLedgerInactivity.stopInactivityMonitor");
        Assert.Null(fixture.Js.StoredAuthentication);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ConfirmActivity_ResetsTimerOnlyAfterSuccessfulRefresh(bool refreshSucceeds)
    {
        await using var fixture = new AuthFixture();
        fixture.Handler.RefreshSucceeds = refreshSucceeds;
        await fixture.Auth.InitializeAsync();

        var result = await fixture.Session.ConfirmSessionActivityAsync();

        Assert.Equal(refreshSucceeds, result.Success);
        Assert.True(fixture.Handler.RefreshRequest?.IsActivityConfirmation);
        Assert.Equal(refreshSucceeds ? 1 : 0,
            fixture.Js.Calls.Count(call => call.Identifier == "houseLedgerInactivity.confirmActivity"));
        Assert.Equal(refreshSucceeds, fixture.Auth.CurrentToken is not null);
    }

    [Fact]
    public async Task DisposedSessionService_UnsubscribesFromInactivityCallbacks()
    {
        await using var fixture = new AuthFixture();
        var warnings = 0;
        fixture.Session.SessionExpiring += () => warnings++;
        await fixture.Auth.InitializeAsync();
        await fixture.Session.DisposeAsync();

        await fixture.Inactivity.OnSessionExpiringAsync();
        await fixture.Inactivity.OnInactivityTimeoutAsync();

        Assert.Equal(0, warnings);
        Assert.Empty(fixture.Handler.RequestedPaths);
        Assert.Null(fixture.Navigation.LastUri);
        Assert.Single(fixture.Js.Calls, call => call.Identifier == "houseLedgerInactivity.stopInactivityMonitor");
    }

    private sealed class AuthFixture : IAsyncDisposable
    {
        private readonly HttpClient _client;
        private readonly AuthStorageService _storage;

        public FakeJsRuntime Js { get; }
        public InactivityService Inactivity { get; }
        public SessionService Session { get; }
        public AuthService Auth { get; }
        public TestNavigationManager Navigation { get; } = new();
        public TestHttpHandler Handler { get; }

        public AuthFixture()
        {
            var token = new TokenResponse
            {
                AccessToken = "test-access-token",
                RefreshToken = "test-refresh-token",
                SessionId = Guid.NewGuid()
            };
            Js = new FakeJsRuntime
            {
                StoredAuthentication = JsonSerializer.Serialize(new { TabId = "test-tab", Token = token })
            };
            Handler = new TestHttpHandler(token);
            _client = new HttpClient(Handler) { BaseAddress = new Uri("https://api.example/") };
            _storage = new AuthStorageService(Js);
            Inactivity = new InactivityService(Js, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(2));
            Session = new SessionService(Inactivity, Navigation, () => Auth);
            Auth = new AuthService(_client, _storage, Session);
        }

        public async ValueTask DisposeAsync()
        {
            await Session.DisposeAsync();
            await _storage.DisposeAsync();
            await Inactivity.DisposeAsync();
            _client.Dispose();
        }
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public string? LastUri { get; private set; }

        public TestNavigationManager() => Initialize("https://client/", "https://client/");

        protected override void NavigateToCore(string uri, bool forceLoad) => LastUri = ToAbsoluteUri(uri).AbsolutePath;
    }

    private sealed class TestHttpHandler(TokenResponse token) : HttpMessageHandler
    {
        public bool RefreshSucceeds { get; set; } = true;
        public RefreshTokenRequest? RefreshRequest { get; private set; }
        public List<string> RequestedPaths { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            RequestedPaths.Add(path);
            if (path == "/api/v1/auth/refresh")
            {
                RefreshRequest = await request.Content!.ReadFromJsonAsync<RefreshTokenRequest>(cancellationToken);
                return RefreshSucceeds
                    ? new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(token) }
                    : new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private sealed class FakeJsRuntime : IJSRuntime
    {
        public string? StoredAuthentication { get; set; }
        public List<JsCall> Calls { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            var arguments = args ?? [];
            Calls.Add(new JsCall(identifier, arguments));
            object? result = identifier switch
            {
                "houseLedgerAuthStorage.getTabId" => "test-tab",
                "houseLedgerAuthStorage.subscribe" => 1,
                "houseLedgerAuthStorage.get" => StoredAuthentication,
                _ => null
            };
            if (identifier == "houseLedgerAuthStorage.set")
            {
                StoredAuthentication = (string?)arguments[1];
            }
            else if (identifier == "houseLedgerAuthStorage.remove")
            {
                StoredAuthentication = null;
            }

            return ValueTask.FromResult(result is null ? default! : (TValue)result);
        }
    }

    private sealed record JsCall(string Identifier, object?[] Arguments);
}
