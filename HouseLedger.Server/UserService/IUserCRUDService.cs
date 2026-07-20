using HouseLedger.Shared.DTO.User;

namespace HouseLedger.Server.UserService
{
    public interface IUserCRUDService
    {
        Task<bool> CreateUser(CreateUserRequest request);
        Task<bool> UpdateUser(UpdateUserRequest request);
        Task<bool> UpdateUserPassword(UpdateUserPasswordRequest request);
        Task<bool> DeleteUser(Guid userId);

    }
}
