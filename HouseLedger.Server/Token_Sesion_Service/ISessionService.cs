using HouseLedger.Shared.DTO.User;
using HouseLedger.Shared.Models;
using HouseLedger.Shared.Response;

namespace HouseLedger.Server.Token_Sesion_Service
{
    public interface ISessionService
    {
        Task<ServiceResponse<UserSession>> CreateSession(FullUserInfo user);
        Task<ServiceResponse<UserSession>> RotateRefreshToken(Guid sessionId, string refreshToken);
        Task<ServiceResponse<bool>> BlockSession(Guid sessionId, string refreshToken);

        Task<ServiceResponse<bool>> ValidateSession(Guid sessionId, string refreshToken);

    }
}
