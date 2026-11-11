using HouseLedger.Shared.DTO.Auth;
using HouseLedger.Shared.Response;

namespace HouseLedger.Server.AuthServices
{
    public interface IAuthService
    {
        Task<ServiceResponse<TokenResponse>> Login(LoginRequest request);
        Task<ServiceResponse<TokenResponse>> Refresh(RefreshTokenRequest request);
        Task<ServiceResponse<bool>> Logout(RefreshTokenRequest request);
    }
}
