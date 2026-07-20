using HouseLedger.Shared.DTO.User;

namespace HouseLedger.Server.UserService
{
    public class UserCRUDService : IUserCRUDService
    {
        public Task<bool> CreateUser(CreateUserRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteUser(Guid userId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateUser(UpdateUserRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateUserPassword(UpdateUserPasswordRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
