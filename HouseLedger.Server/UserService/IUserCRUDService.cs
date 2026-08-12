using HouseLedger.Shared.DTO.User;
using HouseLedger.Shared.Response;

namespace HouseLedger.Server.UserService
{
    public interface IUserCRUDService
    {
        Task<ServiceResponse<bool>> CreateUser(CreateUserRequest request);
        Task<ServiceResponse<bool>> UpdateUser(UpdateUserRequest request,Guid userId);
        Task<ServiceResponse<bool>> UpdateUserPassword(UpdateUserPasswordRequest request,Guid userId);
        Task<ServiceResponse<bool>> DeleteUser(Guid userId);
        Task<ServiceResponse<BasicUserInfo?>> GetUserById(Guid userId);
        Task<ServiceResponse<FullUserInfo?>> GetFullUserById(Guid userId);

    }
}
