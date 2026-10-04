using HouseLedger.Shared.DTO.User;
using HouseLedger.Shared.Models;
using HouseLedger.Shared.Response;

namespace HouseLedger.Server.Token_Sesion_Service
{
    public interface ISessionService
    {
        Task<ServiceResponse<UserSession>> CreateSession(FullUserInfo user);
        Task<ServiceResponse<bool>> BlockSession(Guid sessionId, string refreshToken);

        Task<ServiceResponse<bool>> ValidateSession(Guid sessionId, string refreshToken);

    }
}
