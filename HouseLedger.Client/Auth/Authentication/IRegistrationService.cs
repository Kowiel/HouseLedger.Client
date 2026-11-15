using HouseLedger.Shared.DTO.User;
using HouseLedger.Shared.Response;

namespace HouseLedger.Client.Auth;

public interface IRegistrationService
{
    Task<ServiceResponse<bool>> RegisterAsync(CreateUserRequest request);
}
