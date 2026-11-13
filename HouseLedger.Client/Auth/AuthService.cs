using System.Net.Http.Json;
using HouseLedger.Shared.DTO.Auth;
using HouseLedger.Shared.Response;

namespace HouseLedger.Client.Auth
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthStorageService _authStorageService;
        private readonly SessionService _sessionService;
        private readonly SemaphoreSlim _refreshLock = new(1, 1);
        private readonly SemaphoreSlim _initializationLock = new(1, 1);
        private TokenResponse? _currentToken;
        private bool _initialized;

        public TokenResponse? CurrentToken => _currentToken;

        public Guid? CurrentUserId => JwtReader.GetUserId(_currentToken?.AccessToken);

        public event Action? AuthenticationStateChanged;

        public AuthService(HttpClient httpClient, AuthStorageService authStorageService, SessionService sessionService)
        {
            _httpClient = httpClient;
            _authStorageService = authStorageService;
            _sessionService = sessionService;
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

                _currentToken = await _authStorageService.InitializeAsync(OnAuthenticationStorageChangedAsync);

                _initialized = true;
                await UpdateInactivityMonitorAsync();
            }
            finally
            {
                _initializationLock.Release();
            }
        }

        public async Task<ServiceResponse<TokenResponse>> LoginAsync(LoginRequest request)
        {
            await InitializeAsync();
            using var response = await _httpClient.PostAsJsonAsync("api/v1/auth/login", request);
            if (response.IsSuccessStatusCode)
            {
                var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
                if (token is not null)
                {
                    _currentToken = token;
                    await StoreTokenAsync(token);
                    await UpdateInactivityMonitorAsync();
                    AuthenticationStateChanged?.Invoke();
                }
                else
                {
                    await ClearAuthenticationAsync();
                }
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

        public async Task ClearAuthenticationAsync(string? forcedLogoutMessage = null)
        {
            await InitializeAsync();
            _sessionService.SetForcedLogoutMessage(forcedLogoutMessage);

            var hadToken = _currentToken is not null;
            _currentToken = null;
            await UpdateInactivityMonitorAsync();
            await _authStorageService.ClearOwnedAuthenticationAsync();
            if (hadToken)
            {
                AuthenticationStateChanged?.Invoke();
            }
        }

        private async Task OnAuthenticationStorageChangedAsync(TokenResponse? token)
        {
            var previousToken = _currentToken;
            _currentToken = token;
            await UpdateInactivityMonitorAsync();

            if (previousToken?.AccessToken != token?.AccessToken ||
                previousToken?.RefreshToken != token?.RefreshToken ||
                previousToken?.SessionId != token?.SessionId)
            {
                AuthenticationStateChanged?.Invoke();
            }
        }

        private Task UpdateInactivityMonitorAsync() => _sessionService.SetActiveAsync(_currentToken is not null);

        public async Task<ServiceResponse<TokenResponse>> RefreshAsync(bool force = false, bool isActivityConfirmation = false)
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

                if (!await _authStorageService.IsCurrentTabOwnerAsync())
                {
                    await ClearAuthenticationAsync("This session is active in another tab. Please sign in again.");
                    return new ServiceResponse<TokenResponse>
                    {
                        Data = null,
                        Success = false,
                        Message = "This session is active in another tab."
                    };
                }

                var tokenToRefresh = _currentToken;

                if (!force && !JwtReader.IsAccessTokenExpiring(tokenToRefresh.AccessToken))
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
                    RefreshToken = tokenToRefresh.RefreshToken,
                    IsActivityConfirmation = isActivityConfirmation
                });

                if (!response.IsSuccessStatusCode)
                {
                    await ClearAuthenticationAsync("Your session expired or was revoked. Please sign in again.");
                    var message = await response.Content.ReadAsStringAsync();
                    return new ServiceResponse<TokenResponse>
                    {
                        Data = null,
                        Success = false,
                        Message = string.IsNullOrWhiteSpace(message) ? "Token refresh failed." : message.Trim('"')
                    };
                }

                if (!await _authStorageService.IsCurrentTabOwnerAsync())
                {
                    await ClearAuthenticationAsync("This session is active in another tab. Please sign in again.");
                    return new ServiceResponse<TokenResponse>
                    {
                        Data = null,
                        Success = false,
                        Message = "This session is active in another tab."
                    };
                }

                var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
                if (token is not null)
                {
                    _currentToken = token;
                    await StoreTokenAsync(token);
                    await UpdateInactivityMonitorAsync();
                    AuthenticationStateChanged?.Invoke();
                }
                else
                {
                    await ClearAuthenticationAsync("Your session expired or could not be renewed. Please sign in again.");
                }
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

        private Task StoreTokenAsync(TokenResponse token) => _authStorageService.StoreTokenAsync(token);
    }
}
