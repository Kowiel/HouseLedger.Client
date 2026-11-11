using HouseLedger.Shared.DTO.User;
using HouseLedger.Shared.Response;

namespace HouseLedger.Client.UserService;

public interface IUserService
{
    Task<ServiceResponse<bool>> CreateAsync(CreateUserRequest request);
    Task<ServiceResponse<BasicUserInfo?>> GetBasicAsync();
    Task<ServiceResponse<FullUserInfo?>> GetFullAsync();
    Task<ServiceResponse<bool>> UpdateAsync(UpdateUserRequest request);
    Task<ServiceResponse<bool>> DeleteAsync();
}
