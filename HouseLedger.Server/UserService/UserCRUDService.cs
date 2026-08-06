using HouseLedger.Server.Data;
using HouseLedger.Shared.DTO.User;
using HouseLedger.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.EntityFrameworkCore;
using Superpower.Model;
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

        public async Task<bool> DeleteUser(Guid userId)
        {
           var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
            {
                return false;
            }
            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded;
        }

        public async Task<FullUserInfo?> GetFullUserById(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            var fullUserInfo = user is null ? null : new FullUserInfo
            {
                Id= user.Id,
                UserName = user.UserName,
                NormalizedUserName = user.NormalizedUserName,
                Email = user.Email,
                NormalizedEmail = user.NormalizedEmail,
                PhoneNumber = user.PhoneNumber,
                FirstName = user.FirstName,
                LastName = user.LastName,
                DisplayName = user.DisplayName,
                CreatedDate = user.CreatedDate
            };

            return fullUserInfo;
        }

        public async Task<BasicUserInfo?> GetUserById(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            var basicUserInfo = user is null ? null : new BasicUserInfo
            {
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                FirstName = user.FirstName,
                LastName = user.LastName,
                DisplayName = user.DisplayName
            };

            return basicUserInfo;
        }

        public async Task<bool> UpdateUser(UpdateUserRequest request, Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user is null)
            {
                return false;
            }


            user.DisplayName = request.DisplayName;
            user.Email = request.Email;
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
           
            var result = await _userManager.UpdateAsync(user);

            return result.Succeeded;

        }

        public async Task<bool> UpdateUserPassword(UpdateUserPasswordRequest request, Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user is null)
            {
                return false;
            }
            var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);

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
        private async Task<bool> IsDistinctUser(string email)
        {
            var existingUserByEmail = await _userManager.FindByEmailAsync(email);
            if (existingUserByEmail != null)
            {
                return false;
            }

            return true;
        }
    }
}
