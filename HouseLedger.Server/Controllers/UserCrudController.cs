using HouseLedger.Server.StaticClasses;
using HouseLedger.Server.ToolServices;
using HouseLedger.Server.UserService;
using HouseLedger.Shared.DTO.Auth;
using HouseLedger.Shared.DTO.User;
using HouseLedger.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;



namespace HouseLedger.Server.Controllers
{
    [ApiController]
    [Route(ApiRoutes.UserCRUD)]
    [Authorize]
    public class UserCrudController:ControllerBase
    {
        private readonly IUserCRUDService _userCrudService;

        public UserCrudController(IUserCRUDService userCrudService)
        {
            _userCrudService = userCrudService;
        }

        [HttpGet("me", Name = "GetCurrentUserInfo")]
        public async Task<ActionResult<FullUserInfo>> GetUserInfoAsync()
        {
            var userId = GetCurrentUserId();
            if (userId is null)
            {
                return Unauthorized();
            }

            var userInfo = await _userCrudService.GetFullUserById(userId.Value);
            if (userInfo.Success == false)
            {
                return BadRequest(userInfo.Message);
            }
            return Ok(userInfo.Data);
        }

        [HttpGet("me/basic", Name = "GetCurrentBasicUserInfo")]
        public async Task<ActionResult<BasicUserInfo>> GetUserInfoBasicAsync()
        {
            var userId = GetCurrentUserId();
            if (userId is null)
            {
                return Unauthorized();
            }

            var userInfo = await _userCrudService.GetUserById(userId.Value);
            if (userInfo.Success == false)
            {
                return BadRequest(userInfo.Message);
            }
            return Ok(userInfo.Data);
        }

        [HttpPost("createuser", Name = "CreateUser")]
        [AllowAnonymous]
        public async Task<ActionResult<bool>> CreateUserAsync([FromBody] CreateUserRequest request)
        {
            var result = await _userCrudService.CreateUser(request);
            if (!result.Success)
            {
                return BadRequest(result.Message);
            }
            return Ok(result.Data);
        }

        [HttpDelete("me", Name = "DeleteCurrentUser")]
        public async Task<ActionResult<bool>> DeleteUserAsync()
        {
            var userId = GetCurrentUserId();
            if (userId is null)
            {
                return Unauthorized();
            }

            var result = await _userCrudService.DeleteUser(userId.Value);
            if (!result.Success)
            {
                return BadRequest(result.Message);
            }
            return Ok(result.Data);

        }

        [HttpPut("me", Name = "UpdateCurrentUser")]
        public async Task<ActionResult<bool>> UpdateUserAsync([FromBody] UpdateUserRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
            {
                return Unauthorized();
            }

            var result = await _userCrudService.UpdateUser(request, userId.Value);
            if (!result.Success)
            {
                return BadRequest(result.Message);
            }
            return Ok(result.Data);
        }
        [HttpPut("me/password", Name = "UpdateCurrentUserPassword")]
        public async Task<ActionResult<bool>> UpdateUserPasswordAsync([FromBody] UpdateUserPasswordRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
            {
                return Unauthorized();
            }

            var result = await _userCrudService.UpdateUserPassword(request, userId.Value);
            if (!result.Success)
            {
                return BadRequest(result.Message);
            }
            return Ok(result.Data);
        }

        private Guid? GetCurrentUserId()
        {
            var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            return Guid.TryParse(claimValue, out var currentUserId) ? currentUserId : null;
        }
    }
}
