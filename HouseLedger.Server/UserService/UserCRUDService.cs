using HouseLedger.Server.Data;
using HouseLedger.Shared.DTO.User;
using HouseLedger.Shared.Models;
using HouseLedger.Shared.Response;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace HouseLedger.Server.UserService
{
    public class UserCRUDService : IUserCRUDService
    {
        private readonly HouseLedgerDbContext _dbContext;
        private readonly UserManager<AppUser> _userManager;

        public UserCRUDService(
            HouseLedgerDbContext dbContext,
            UserManager<AppUser> userManager)
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
                DisplayName = request.UserName.ToLowerInvariant(),
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
            var user = await _dbContext.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(currentUser => currentUser.Id == userId);

            var fullUserInfo = user is null
                ? null
                : FullUserInfo.FromEntity(user);

            return new ServiceResponse<FullUserInfo?>
            {
                Data = fullUserInfo,
                Success = fullUserInfo is not null,
                Message = fullUserInfo is not null ? "User found." : "User not found."
            };
        }

        public async Task<ServiceResponse<BasicUserInfo?>> GetUserById(Guid userId)
        {
            var fullUser = await GetFullUserById(userId);
            var basicUserInfo = fullUser.Data is null
                ? null
                : BasicUserInfo.FromEntity(fullUser.Data);

            return new ServiceResponse<BasicUserInfo?>
            {
                Data = basicUserInfo,
                Success = basicUserInfo is not null,
                Message = basicUserInfo is not null ? "User found." : "User not found."
            };
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
            user.PhoneNumber = request.PhoneNumber;
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;

            var result = await _userManager.UpdateAsync(user);

            return new ServiceResponse<bool> { Data = result.Succeeded, Success = result.Succeeded, Message = result.Succeeded ? "User updated successfully." : $"Failed to update user: {string.Join(", ", result.Errors.Select(e => e.Description))}" };

        }

        public async Task<ServiceResponse<bool>> UpdateUserPassword(UpdateUserPasswordRequest request, Guid userId)
        {
            if (!string.Equals(request.NewPassword, request.RetypeNewPassword, StringComparison.Ordinal))
            {
                return new ServiceResponse<bool>
                {
                    Data = false,
                    Success = false,
                    Message = "Passwords do not match."
                };
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user is null)
            {
                return new ServiceResponse<bool> { Data = false, Success = false, Message = "User not found." };
            }
            var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);

            return new ServiceResponse<bool> { Data = result.Succeeded, Success = result.Succeeded, Message = result.Succeeded ? "User password updated successfully." : $"Failed to update user password: {string.Join(", ", result.Errors.Select(e => e.Description))}" };
        }

        public async Task<ServiceResponse<UserSettingsDto?>> GetUserSettings(Guid userId)
        {
            var user = await _dbContext.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(currentUser => currentUser.Id == userId);

            if (user is null)
            {
                return new ServiceResponse<UserSettingsDto?>
                {
                    Data = null,
                    Success = false,
                    Message = "User not found."
                };
            }

            return new ServiceResponse<UserSettingsDto?>
            {
                Data = new UserSettingsDto
                {
                    DarkModeEnabled = user.DarkModeEnabled,
                    TextSize = string.IsNullOrWhiteSpace(user.TextSize) ? "Normal" : user.TextSize,
                    EmailNotificationsEnabled = user.EmailNotificationsEnabled,
                    WeeklySummaryEnabled = user.WeeklySummaryEnabled
                },
                Success = true,
                Message = "User settings loaded successfully."
            };
        }

        public async Task<ServiceResponse<bool>> UpdateUserSettings(UpdateUserSettingsRequest request, Guid userId)
        {
            if (!IsSupportedTextSize(request.TextSize))
            {
                return new ServiceResponse<bool>
                {
                    Data = false,
                    Success = false,
                    Message = "Text size must be Small, Normal, or Large."
                };
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
            {
                return new ServiceResponse<bool>
                {
                    Data = false,
                    Success = false,
                    Message = "User not found."
                };
            }

            user.DarkModeEnabled = request.DarkModeEnabled;
            user.TextSize = request.TextSize;
            user.EmailNotificationsEnabled = request.EmailNotificationsEnabled;
            user.WeeklySummaryEnabled = request.WeeklySummaryEnabled;

            var result = await _userManager.UpdateAsync(user);
            return new ServiceResponse<bool>
            {
                Data = result.Succeeded,
                Success = result.Succeeded,
                Message = result.Succeeded
                    ? "User settings updated successfully."
                    : $"Failed to update user settings: {string.Join(", ", result.Errors.Select(e => e.Description))}"
            };
        }


        private async Task<bool> IsDistinctUser(string email, string userName)
        {
            var normalizedEmail = _userManager.NormalizeEmail(email);
            var normalizedUserName = _userManager.NormalizeName(userName);

            return !await _dbContext.Users
                .AsNoTracking()
                .AnyAsync(user => user.NormalizedEmail == normalizedEmail || user.NormalizedUserName == normalizedUserName);
        }

        private static bool IsSupportedTextSize(string value) =>
            value is "Small" or "Normal" or "Large";
    }
}
