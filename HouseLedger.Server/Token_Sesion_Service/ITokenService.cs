using HouseLedger.Shared.Models;

namespace HouseLedger.Server.Token_Sesion_Service
{
    public interface ITokenService
    {
        string CreateAccessToken(AppUser user, UserSession session);
    }
}
