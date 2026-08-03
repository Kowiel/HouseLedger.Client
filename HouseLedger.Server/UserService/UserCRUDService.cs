using HouseLedger.Server.Data;
using HouseLedger.Shared.DTO.User;
using HouseLedger.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Diagnostics;

namespace HouseLedger.Server.UserService
{
    public class UserCRUDService : IUserCRUDService
    {
        private readonly HouseLedgerDbContext _dbContext;
        private readonly UserManager<AppUser> _userManager;
        public UserCRUDService(HouseLedgerDbContext dbContext, UserManager<AppUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }
        public async Task<bool> CreateUser(CreateUserRequest request)
        {
            if (await IsDistinctUser(request.Email, request.UserName) == false)
            {
                return false;
            }

            var newUser = new AppUser
            {
                UserName = request.UserName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                FirstName = request.FirstName,
                LastName = request.LastName,
                DisplayName =request.UserName.ToLower(),
                CreatedDate = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(newUser, request.Password);

            if(!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                Debug.WriteLine($"Failed to create user: {errors}");
                return false;
            }

            return result.Succeeded;
        }

        private async Task<bool> IsDistinctUser(string email, string userName)
        {
            var existingUserByEmail = await _userManager.FindByEmailAsync(email);
            if (existingUserByEmail != null)
            {
                return false;
            }

            var existingUserByName = await _userManager.FindByNameAsync(userName);
            if (existingUserByName != null)
            {
                return false;
            }

            return true;
        }

        public Task<bool> DeleteUser(Guid userId)
        {
            throw new NotImplementedException();
        }

        public Task<FullUserInfo?> GetFullUserById(Guid userId)
        {
            throw new NotImplementedException();
        }

        public Task<BasicUserInfo?> GetUserById(Guid userId)
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
