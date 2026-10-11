using System.Net.Http.Json;
using System.Text.Json;
using HouseLedger.Shared.DTO.Auth;
using HouseLedger.Shared.DTO.User;
using HouseLedger.Shared.Response;

namespace HouseLedger.Client.Auth
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly SemaphoreSlim _refreshLock = new(1, 1);
        private TokenResponse? _currentToken;

        public TokenResponse? CurrentToken => _currentToken;

        public AuthService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ServiceResponse<bool>> RegisterAsync(CreateUserRequest request)
        {
            using var response = await _httpClient.PostAsJsonAsync("api/v1/user/crud/createuser", request);
            Console.WriteLine(response);
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
            using var response = await _httpClient.PostAsJsonAsync("api/v1/auth/login", request);
            if (response.IsSuccessStatusCode)
            {
                var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
                _currentToken = token;
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

        public void ClearAuthentication()
        {
            _currentToken = null;
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

                if (!force && !IsAccessTokenExpiring(_currentToken.AccessToken))
                {
                    return new ServiceResponse<TokenResponse>
                    {
                        Data = _currentToken,
                        Success = true,
                        Message = "The current access token is still valid."
                    };
                }

                using var response = await _httpClient.PostAsJsonAsync("api/v1/auth/refresh", new RefreshTokenRequest
                {
                    SessionId = _currentToken.SessionId,
                    RefreshToken = _currentToken.RefreshToken
                });

                if (!response.IsSuccessStatusCode)
                {
                    _currentToken = null;
                    var message = await response.Content.ReadAsStringAsync();
                    return new ServiceResponse<TokenResponse>
                    {
                        Data = null,
                        Success = false,
                        Message = string.IsNullOrWhiteSpace(message) ? "Token refresh failed." : message.Trim('"')
                    };
                }

                var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
                _currentToken = token;
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
            if (_currentToken is null)
            {
                return new ServiceResponse<bool>
                {
                    Data = true,
                    Success = true,
                    Message = "Already logged out."
                };
            }

            using var response = await _httpClient.PostAsJsonAsync("api/v1/auth/logout", new RefreshTokenRequest
            {
                SessionId = _currentToken.SessionId,
                RefreshToken = _currentToken.RefreshToken
            });

            _currentToken = null;
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
    }
}
