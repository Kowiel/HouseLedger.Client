using HouseLedger.Server.StaticClasses;
using HouseLedger.Server.AuthServices;
using HouseLedger.Shared.DTO.Auth;
using Microsoft.AspNetCore.Mvc;

namespace HouseLedger.Server.Controllers
{
    [ApiController]
    [Route(ApiRoutes.Autorization)]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login", Name = "Login")]
        public async Task<ActionResult<TokenResponse>> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.Login(request);
            if (!result.Success || result.Data is null)
            {
                return Unauthorized(result.Message);
            }

            return Ok(result.Data);
        }

        [HttpPost("refresh", Name = "RefreshToken")]
        public async Task<ActionResult<TokenResponse>> Refresh([FromBody] RefreshTokenRequest request)
        {
            var result = await _authService.Refresh(request);
            if (!result.Success || result.Data is null)
            {
                return Unauthorized(result.Message);
            }

            return Ok(result.Data);
        }

        [HttpPost("logout", Name = "Logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
        {
            var result = await _authService.Logout(request);
            if (!result.Success)
            {
                return Unauthorized(result.Message);
            }

            return NoContent();
        }
    }
}