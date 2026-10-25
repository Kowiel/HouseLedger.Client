using System.Net.Http.Json;
using System.Text.Json;
using HouseLedger.Shared.DTO.Auth;
using HouseLedger.Shared.DTO.User;
using HouseLedger.Shared.Response;
using Microsoft.JSInterop;

namespace HouseLedger.Client.Auth
{
    public class AuthService : IAuthService, IAsyncDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly IJSRuntime _jsRuntime;
        private readonly SemaphoreSlim _refreshLock = new(1, 1);
        private readonly SemaphoreSlim _initializationLock = new(1, 1);
        private TokenResponse? _currentToken;
        private DotNetObjectReference<AuthService>? _storageCallbackReference;
        private string? _tabId;
        private int _storageSubscriptionId;
        private bool _initialized;
        private const string LocalStorageKey = "houseLedger.authentication";
        private const string LegacySessionStorageKey = "houseLedger.tokenResponse";

        public TokenResponse? CurrentToken => _currentToken;

        public Guid? CurrentUserId => GetUserId(_currentToken?.AccessToken);

        public event Action? AuthenticationStateChanged;

        public AuthService(HttpClient httpClient, IJSRuntime jsRuntime)
        {
            _httpClient = httpClient;
            _jsRuntime = jsRuntime;
        }

        public async Task InitializeAsync()
        {
            if (_initialized)
            {
                return;
            }

            await _initializationLock.WaitAsync();
            try
            {
                if (_initialized)
                {
                    return;
                }

                _tabId = await _jsRuntime.InvokeAsync<string>("houseLedgerAuthStorage.getTabId");
                _storageCallbackReference = DotNetObjectReference.Create(this);
                _storageSubscriptionId = await _jsRuntime.InvokeAsync<int>(
                    "houseLedgerAuthStorage.subscribe",
                    LocalStorageKey,
                    _storageCallbackReference);

                var storedAuthentication = await _jsRuntime.InvokeAsync<string?>("houseLedgerAuthStorage.get", LocalStorageKey);
                if (!string.IsNullOrWhiteSpace(storedAuthentication))
                {
                    var session = DeserializeStoredAuthentication(storedAuthentication);
                    if (session?.Token is not null && session.TabId == _tabId)
                    {
                        _currentToken = session.Token;
                    }
                    else if (session is null || string.IsNullOrWhiteSpace(session.TabId) || session.Token is null)
                    {
                        await _jsRuntime.InvokeVoidAsync("houseLedgerAuthStorage.remove", LocalStorageKey);
                    }
                }
                else
                {
                    await MigrateLegacySessionTokenAsync();
                }

                _initialized = true;
            }
            finally
            {
                _initializationLock.Release();
            }
        }

        public async Task<ServiceResponse<bool>> RegisterAsync(CreateUserRequest request)
        {
            using var response = await _httpClient.PostAsJsonAsync("api/v1/user/crud/createuser", request);
            if (response.IsSuccessStatusCode)
            {
                var created = await response.Content.ReadFromJsonAsync<bool>();
                return new ServiceResponse<bool>
                {
                    Data = created,
                    Success = created,
                    Message = created ? "Account created successfully." : "The account could not be created."
                };
            }

            var message = await response.Content.ReadAsStringAsync();
            return new ServiceResponse<bool>
            {
                Data = false,
                Success = false,
                Message = string.IsNullOrWhiteSpace(message) ? "Registration failed." : message.Trim('"')
            };
        }

        public async Task<ServiceResponse<TokenResponse>> LoginAsync(LoginRequest request)
        {
            await InitializeAsync();
            using var response = await _httpClient.PostAsJsonAsync("api/v1/auth/login", request);
            if (response.IsSuccessStatusCode)
            {
                var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
                _currentToken = token;
                if (token is not null)
                {
                    await StoreTokenAsync(token);
                }
                else
                {
                    await ClearAuthenticationAsync();
                }
                AuthenticationStateChanged?.Invoke();
                return new ServiceResponse<TokenResponse>
                {
                    Data = token,
                    Success = token is not null,
                    Message = token is not null ? "Login successful." : "The login response was empty."
                };
            }

            var message = await response.Content.ReadAsStringAsync();
            return new ServiceResponse<TokenResponse>
            {
                Data = null,
                Success = false,
                Message = string.IsNullOrWhiteSpace(message) ? "Login failed." : message.Trim('"')
            };
        }

        public async Task ClearAuthenticationAsync()
        {
            await InitializeAsync();
            _currentToken = null;
            var session = await GetStoredAuthenticationAsync();
            if (session?.TabId == _tabId)
            {
                await _jsRuntime.InvokeVoidAsync("houseLedgerAuthStorage.remove", LocalStorageKey);
            }
            AuthenticationStateChanged?.Invoke();
        }

        public async ValueTask DisposeAsync()
        {
            if (_storageSubscriptionId != 0)
            {
                await _jsRuntime.InvokeVoidAsync("houseLedgerAuthStorage.unsubscribe", _storageSubscriptionId);
            }

            _storageCallbackReference?.Dispose();
        }

        [JSInvokable]
        public Task OnAuthStorageChanged(string? storedAuthentication)
        {
            var session = DeserializeStoredAuthentication(storedAuthentication);
            var token = session is not null && session.TabId == _tabId ? session.Token : null;
            var previousToken = _currentToken;
            _currentToken = token;

            if (previousToken?.AccessToken != token?.AccessToken ||
                previousToken?.RefreshToken != token?.RefreshToken ||
                previousToken?.SessionId != token?.SessionId)
            {
                AuthenticationStateChanged?.Invoke();
            }

            return Task.CompletedTask;
        }

        private static Guid? GetUserId(string? accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return null;
            }

            var tokenParts = accessToken.Split('.');
            if (tokenParts.Length != 3)
            {
                return null;
            }

            try
            {
                var payload = tokenParts[1].Replace('-', '+').Replace('_', '/');
                payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
                using var document = JsonDocument.Parse(Convert.FromBase64String(payload));

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

        private static bool IsAccessTokenExpiring(string accessToken)
        {
            var parts = accessToken.Split('.');
            if (parts.Length != 3)
            {
                return true;
            }

            try
            {
                var payload = parts[1].Replace('-', '+').Replace('_', '/');
                payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
                using var document = JsonDocument.Parse(Convert.FromBase64String(payload));
                if (!document.RootElement.TryGetProperty("exp", out var expiration))
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

        public async Task<ServiceResponse<TokenResponse>> RefreshAsync(bool force = false)
        {
            await InitializeAsync();
            await _refreshLock.WaitAsync();
            try
            {
                if (_currentToken is null)
                {
                    return new ServiceResponse<TokenResponse>
                    {
                        Data = null,
                        Success = false,
                        Message = "There is no active session to refresh."
                    };
                }

                if (!await IsCurrentTabOwnerAsync())
                {
                    await ClearAuthenticationAsync();
                    return new ServiceResponse<TokenResponse>
                    {
                        Data = null,
                        Success = false,
                        Message = "This session is active in another tab."
                    };
                }

                var tokenToRefresh = _currentToken;

                if (!force && !IsAccessTokenExpiring(tokenToRefresh.AccessToken))
                {
                    return new ServiceResponse<TokenResponse>
                    {
                        Data = tokenToRefresh,
                        Success = true,
                        Message = "The current access token is still valid."
                    };
                }

                using var response = await _httpClient.PostAsJsonAsync("api/v1/auth/refresh", new RefreshTokenRequest
                {
                    SessionId = tokenToRefresh.SessionId,
                    RefreshToken = tokenToRefresh.RefreshToken
                });

                if (!response.IsSuccessStatusCode)
                {
                    await ClearAuthenticationAsync();
                    var message = await response.Content.ReadAsStringAsync();
                    return new ServiceResponse<TokenResponse>
                    {
                        Data = null,
                        Success = false,
                        Message = string.IsNullOrWhiteSpace(message) ? "Token refresh failed." : message.Trim('"')
                    };
                }

                if (!await IsCurrentTabOwnerAsync())
                {
                    await ClearAuthenticationAsync();
                    return new ServiceResponse<TokenResponse>
                    {
                        Data = null,
                        Success = false,
                        Message = "This session is active in another tab."
                    };
                }

                var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
                _currentToken = token;
                if (token is not null)
                {
                    await StoreTokenAsync(token);
                }
                else
                {
                    await ClearAuthenticationAsync();
                }
                AuthenticationStateChanged?.Invoke();
                return new ServiceResponse<TokenResponse>
                {
                    Data = token,
                    Success = token is not null,
                    Message = token is not null ? "Token refreshed successfully." : "The refresh response was empty."
                };
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        public async Task<ServiceResponse<bool>> LogoutAsync()
        {
            await InitializeAsync();
            if (_currentToken is null)
            {
                await ClearAuthenticationAsync();
                return new ServiceResponse<bool>
                {
                    Data = true,
                    Success = true,
                    Message = "Already logged out."
                };
            }

            var tokenToRevoke = _currentToken;
            await ClearAuthenticationAsync();
            using var response = await _httpClient.PostAsJsonAsync("api/v1/auth/logout", new RefreshTokenRequest
            {
                SessionId = tokenToRevoke.SessionId,
                RefreshToken = tokenToRevoke.RefreshToken
            });

            var message = await response.Content.ReadAsStringAsync();
            return new ServiceResponse<bool>
            {
                Data = response.IsSuccessStatusCode,
                Success = response.IsSuccessStatusCode,
                Message = response.IsSuccessStatusCode
                    ? "Logout successful."
                    : (string.IsNullOrWhiteSpace(message) ? "Logout failed." : message.Trim('"'))
            };
        }

        private async Task StoreTokenAsync(TokenResponse token)
        {
            await _jsRuntime.InvokeVoidAsync(
                "houseLedgerAuthStorage.set",
                LocalStorageKey,
                JsonSerializer.Serialize(new StoredAuthentication
                {
                    TabId = _tabId!,
                    Token = token
                }));
        }

        private async Task MigrateLegacySessionTokenAsync()
        {
            var legacyTokenJson = await _jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", LegacySessionStorageKey);
            if (!string.IsNullOrWhiteSpace(legacyTokenJson))
            {
                try
                {
                    var token = JsonSerializer.Deserialize<TokenResponse>(legacyTokenJson);
                    if (token is not null)
                    {
                        _currentToken = token;
                        await StoreTokenAsync(token);
                    }
                }
                catch (JsonException)
                {
                }

                await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", LegacySessionStorageKey);
            }
        }

        private async Task<StoredAuthentication?> GetStoredAuthenticationAsync()
        {
            var storedAuthentication = await _jsRuntime.InvokeAsync<string?>("houseLedgerAuthStorage.get", LocalStorageKey);
            return DeserializeStoredAuthentication(storedAuthentication);
        }

        private async Task<bool> IsCurrentTabOwnerAsync()
        {
            var session = await GetStoredAuthenticationAsync();
            return session is not null && session.TabId == _tabId && session.Token is not null;
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
}
