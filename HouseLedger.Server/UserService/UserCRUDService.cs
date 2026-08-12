using HouseLedger.Server.Data;
using HouseLedger.Shared.DTO.User;
using HouseLedger.Shared.Models;
using HouseLedger.Shared.Response;
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
        public async Task<ServiceResponse<bool>> CreateUser(CreateUserRequest request)
        {
            if (await IsDistinctUser(request.Email, request.UserName) == false)
            {
                return new ServiceResponse<bool> { Data = false, Success = false, Message = "User with the same email or username already exists." };
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
                return new ServiceResponse<bool> { Data = false, Success = false, Message = $"Failed to create user: {errors}" };
            }

            return new ServiceResponse<bool> { Data = true, Success = true, Message = "User created successfully." };
        }

        public async Task<ServiceResponse<bool>> DeleteUser(Guid userId)
        {
           var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
            {
                return new ServiceResponse<bool> { Data = false, Success = false, Message = "User not found." };
            }
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return new ServiceResponse<bool> { Data = false, Success = false, Message = $"Failed to delete user: {result.Errors}" };
            }
            return new ServiceResponse<bool> { Data = true, Success = true, Message = "User deleted successfully." };
        }

        public async Task<ServiceResponse<FullUserInfo?>> GetFullUserById(Guid userId)
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

            return new ServiceResponse<FullUserInfo?> { Data = fullUserInfo, Success = fullUserInfo != null ? true : false, Message = fullUserInfo != null ? "User found." : "User not found." };
        }

        public async Task<ServiceResponse<BasicUserInfo?>> GetUserById(Guid userId)
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

            return new ServiceResponse<BasicUserInfo?> { Data = basicUserInfo, Success = basicUserInfo != null ? true : false, Message = basicUserInfo != null ? "User found." : "User not found." };
        }

        public async Task<ServiceResponse<bool>> UpdateUser(UpdateUserRequest request, Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user is null)
            {
                return new ServiceResponse<bool> { Data = false, Success = false, Message = "User not found." };
            }


            user.DisplayName = request.DisplayName;
            user.Email = request.Email;
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
           
            var result = await _userManager.UpdateAsync(user);

            return new ServiceResponse<bool> { Data = result.Succeeded, Success = result.Succeeded, Message = result.Succeeded ? "User updated successfully." : $"Failed to update user: {string.Join(", ", result.Errors.Select(e => e.Description))}" };

        }

        public async Task<ServiceResponse<bool>> UpdateUserPassword(UpdateUserPasswordRequest request, Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user is null)
            {
                return new ServiceResponse<bool> { Data = false, Success = false, Message = "User not found." };
            }
            var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);

            return new ServiceResponse<bool> { Data = result.Succeeded, Success = result.Succeeded, Message = result.Succeeded ? "User password updated successfully." : $"Failed to update user password: {string.Join(", ", result.Errors.Select(e => e.Description))}" };
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
