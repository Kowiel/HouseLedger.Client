using HouseLedger.Server.StaticClasses;
using HouseLedger.Server.ToolServices;
using HouseLedger.Server.UserService;
using HouseLedger.Shared.DTO.User;
using HouseLedger.Shared.Models;
using Microsoft.AspNetCore.Mvc;



namespace HouseLedger.Server.Controllers
{
    [ApiController]
    [Route(ApiRoutes.UserCRUD)]
    public class UserCrudController:ControllerBase
    {
        private readonly IUserCRUDService _userCrudService;

        public UserCrudController(IUserCRUDService userCrudService)
        {
            _userCrudService = userCrudService;
        }

        [HttpGet("getFullUserInfo/{userId:guid}", Name = "GetFullUserInfo")]
        public async Task<ActionResult<FullUserInfo>> GetUserInfoAsync([FromRoute] Guid userId)
        {
            var userInfo = await _userCrudService.GetFullUserById(userId);
            if (userInfo.Success == false)
            {
                return BadRequest(userInfo.Message);
            }
            return Ok(userInfo.Data);
        }

        [HttpGet("getBasicUserInfo/{userId:guid}", Name = "GetBasicUserInfo")]
        public async Task<ActionResult<BasicUserInfo>> GetUserInfoBasicAsync([FromRoute] Guid userId)
        {
            var userInfo = await _userCrudService.GetUserById(userId);
            if (userInfo.Success == false)
            {
                return BadRequest(userInfo.Message);
            }
            return Ok(userInfo.Data);
        }

        [HttpPost("createuser", Name = "CreateUser")]
        public async Task<ActionResult<bool>> CreateUserAsync([FromBody] CreateUserRequest request)
        {
            var result = await _userCrudService.CreateUser(request);
            if (!result.Success)
            {
                return BadRequest(result.Message);
            }
            return Ok(result.Data);
        }

        [HttpDelete("deleteuser/{userId:guid}", Name = "DeleteUser")]
        public async Task<ActionResult<bool>> DeleteUserAsync([FromRoute] Guid userId)
        {
            var result = await _userCrudService.DeleteUser(userId);
            if (!result.Success)
            {
                return BadRequest(result.Message);
            }
            return Ok(result.Data);

        }

        [HttpPut("updateuser/{userId:guid}", Name = "UpdateUser")]
        public async Task<ActionResult<bool>> UpdateUserAsync([FromRoute] Guid userId, [FromBody] UpdateUserRequest request)
        {
            var result = await _userCrudService.UpdateUser(request, userId);
            if (!result.Success)
            {
                return BadRequest(result.Message);
            }
            return Ok(result.Data);
        }
        [HttpPut("updateuserpassword/{userId:guid}", Name = "UpdateUserPassword")]
        public async Task<ActionResult<bool>> UpdateUserPasswordAsync([FromRoute] Guid userId, [FromBody] UpdateUserPasswordRequest request)
        {
            var result = await _userCrudService.UpdateUserPassword(request, userId);
            if (!result.Success)
            {
                return BadRequest(result.Message);
            }
            return Ok(result.Data);
        }
    }
}
