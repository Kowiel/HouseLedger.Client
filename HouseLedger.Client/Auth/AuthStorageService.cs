using System.Text.Json;
using HouseLedger.Shared.DTO.Auth;
using Microsoft.JSInterop;

namespace HouseLedger.Client.Auth;

public sealed class AuthStorageService : IAsyncDisposable
{
    private const string LocalStorageKey = "houseLedger.authentication";
    private const string LegacySessionStorageKey = "houseLedger.tokenResponse";
    private readonly IJSRuntime _jsRuntime;
    private DotNetObjectReference<AuthStorageService>? _callbackReference;
    private Func<TokenResponse?, Task>? _authenticationChanged;
    private TokenResponse? _currentToken;
    private string? _tabId;
    private int _storageSubscriptionId;
    private bool _initialized;

    public AuthStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<TokenResponse?> InitializeAsync(Func<TokenResponse?, Task> authenticationChanged)
    {
        if (_initialized)
        {
            return _currentToken;
        }

        _authenticationChanged = authenticationChanged;
        _tabId = await _jsRuntime.InvokeAsync<string>("houseLedgerAuthStorage.getTabId");
        await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", LegacySessionStorageKey);
        _callbackReference = DotNetObjectReference.Create(this);
        _storageSubscriptionId = await _jsRuntime.InvokeAsync<int>(
            "houseLedgerAuthStorage.subscribe",
            LocalStorageKey,
            _callbackReference);

        var storedAuthentication = await _jsRuntime.InvokeAsync<string?>("houseLedgerAuthStorage.get", LocalStorageKey);
        if (!string.IsNullOrWhiteSpace(storedAuthentication))
        {
            var session = DeserializeStoredAuthentication(storedAuthentication);
            if (session?.Token is not null && session.TabId == _tabId)
            {
                _currentToken = session.Token;
                _initialized = true;
                return session.Token;
            }

            if (session is null || string.IsNullOrWhiteSpace(session.TabId) || session.Token is null)
            {
                await _jsRuntime.InvokeVoidAsync("houseLedgerAuthStorage.remove", LocalStorageKey);
            }
        }
        _initialized = true;
        return null;
    }

    public async Task StoreTokenAsync(TokenResponse token)
    {
        await _jsRuntime.InvokeVoidAsync(
            "houseLedgerAuthStorage.set",
            LocalStorageKey,
            JsonSerializer.Serialize(new StoredAuthentication
            {
                TabId = _tabId!,
                Token = token
            }));
        _currentToken = token;
    }

    public async Task ClearOwnedAuthenticationAsync()
    {
        var session = await GetCurrentAuthenticationAsync();
        if (session?.TabId == _tabId)
        {
            await _jsRuntime.InvokeVoidAsync("houseLedgerAuthStorage.remove", LocalStorageKey);
            _currentToken = null;
        }
    }

    public async Task<bool> IsCurrentTabOwnerAsync()
    {
        var session = await GetCurrentAuthenticationAsync();
        return session is not null && session.TabId == _tabId && session.Token is not null;
    }

    [JSInvokable]
    public async Task OnAuthStorageChanged(string? storedAuthentication)
    {
        var session = DeserializeStoredAuthentication(storedAuthentication);
        var token = session is not null && session.TabId == _tabId ? session.Token : null;
        _currentToken = token;
        if (_authenticationChanged is not null)
        {
            await _authenticationChanged(token);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_storageSubscriptionId != 0)
        {
            await _jsRuntime.InvokeVoidAsync("houseLedgerAuthStorage.unsubscribe", _storageSubscriptionId);
        }

        _callbackReference?.Dispose();
    }

    private async Task<StoredAuthentication?> GetCurrentAuthenticationAsync()
    {
        var storedAuthentication = await _jsRuntime.InvokeAsync<string?>("houseLedgerAuthStorage.get", LocalStorageKey);
        return DeserializeStoredAuthentication(storedAuthentication);
    }

    private static StoredAuthentication? DeserializeStoredAuthentication(string? storedAuthentication)
    {
        if (string.IsNullOrWhiteSpace(storedAuthentication))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<StoredAuthentication>(storedAuthentication);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed class StoredAuthentication
    {
        public string TabId { get; set; } = string.Empty;
        public TokenResponse? Token { get; set; }
    }
}
