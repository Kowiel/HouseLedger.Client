using HouseLedger.Shared.DTO.Auth;
using HouseLedger.Shared.Response;

namespace HouseLedger.Client.Auth
{
    public interface IAuthService
    {
        TokenResponse? CurrentToken { get; }
        Guid? CurrentUserId { get; }
        event Action? AuthenticationStateChanged;
        Task InitializeAsync();
        Task<ServiceResponse<TokenResponse>> LoginAsync(LoginRequest request);
        Task<ServiceResponse<TokenResponse>> RefreshAsync(bool force = false, bool isActivityConfirmation = false);
        Task<ServiceResponse<bool>> LogoutAsync();
        Task ClearAuthenticationAsync(string? forcedLogoutMessage = null);
    }
}
