using HouseLedger.Shared.DTO.User;

namespace HouseLedger.Server.UserService
{
    public interface IUserCRUDService
    {
        Task<bool> CreateUser(CreateUserRequest request);
        Task<bool> UpdateUser(UpdateUserRequest request,Guid userId);
        Task<bool> UpdateUserPassword(UpdateUserPasswordRequest request,Guid userId);
        Task<bool> DeleteUser(Guid userId);
        Task<BasicUserInfo?> GetUserById(Guid userId);
        Task<FullUserInfo?> GetFullUserById(Guid userId);

    }
}
