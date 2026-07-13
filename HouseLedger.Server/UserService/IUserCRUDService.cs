using HouseLedger.Shared.DTO;

namespace HouseLedger.Server.UserService
{
    public interface IUserCRUDService
    {
        Task<bool> CreateUser(CreateUserRequest request);
        Task<bool> UpdateUser(UpdateUserRequest request); //LAter return a DTO with the updated user information
        Task<bool> DeleteUser(Guid userId);

    }
}
