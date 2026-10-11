using HouseLedger.Shared.DTO.Auth;
using HouseLedger.Shared.DTO.User;
using HouseLedger.Shared.Response;

namespace HouseLedger.Client.Auth
{
    public interface IAuthService
    {
        TokenResponse? CurrentToken { get; }
        Guid? CurrentUserId { get; }
        event Action? AuthenticationStateChanged;
        Task<ServiceResponse<bool>> RegisterAsync(CreateUserRequest request);
        Task<ServiceResponse<TokenResponse>> LoginAsync(LoginRequest request);
        Task<ServiceResponse<TokenResponse>> RefreshAsync(bool force = false);
        Task<ServiceResponse<bool>> LogoutAsync();
        void ClearAuthentication();
    }
}
