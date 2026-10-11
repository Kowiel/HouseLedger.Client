using HouseLedger.Server.Token_Sesion_Service;
using HouseLedger.Shared.DTO.Auth;
using HouseLedger.Shared.DTO.User;
using HouseLedger.Shared.Models;
using HouseLedger.Shared.Response;
using Microsoft.AspNetCore.Identity;

namespace HouseLedger.Server.AuthServices
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly ISessionService _sessionService;

        public AuthService(
            UserManager<AppUser> userManager,
            ITokenService tokenService,
            ISessionService sessionService)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _sessionService = sessionService;
        }

        public async Task<ServiceResponse<TokenResponse>> Login(LoginRequest request)
        {
            var appUser = await _userManager.FindByEmailAsync(request.Email);
            if (appUser is null || !await _userManager.CheckPasswordAsync(appUser, request.Password))
            {
                return new ServiceResponse<TokenResponse>
                {
                    Data = null,
                    Success = false,
                    Message = "Invalid email or password."
                };
            }

            var user = FullUserInfo.FromEntity(appUser);
            var sessionResponse = await _sessionService.CreateSession(user);
            if (!sessionResponse.Success || sessionResponse.Data is null)
            {
                return new ServiceResponse<TokenResponse>
                {
                    Data = null,
                    Success = false,
                    Message = sessionResponse.Message ?? "Session could not be created."
                };
            }

            var token = _tokenService.CreateAccessToken(appUser, sessionResponse.Data);
            return new ServiceResponse<TokenResponse>
            {
                Data = new TokenResponse
                {
                    AccessToken = token,
                    RefreshToken = sessionResponse.Data.RefreshToken,
                    SessionId = sessionResponse.Data.Id
                },
                Success = true,
                Message = "Login successful."
            };
        }

        public async Task<ServiceResponse<TokenResponse>> Refresh(RefreshTokenRequest request)
        {
            var sessionResponse = await _sessionService.RotateRefreshToken(request.SessionId, request.RefreshToken);
            if (!sessionResponse.Success || sessionResponse.Data is null)
            {
                return new ServiceResponse<TokenResponse>
                {
                    Data = null,
                    Success = false,
                    Message = sessionResponse.Message ?? "The refresh token is invalid."
                };
            }

            var user = await _userManager.FindByIdAsync(sessionResponse.Data.UserId.ToString());
            if (user is null)
            {
                return new ServiceResponse<TokenResponse>
                {
                    Data = null,
                    Success = false,
                    Message = "The session user could not be found."
                };
            }

            return new ServiceResponse<TokenResponse>
            {
                Data = new TokenResponse
                {
                    AccessToken = _tokenService.CreateAccessToken(user, sessionResponse.Data),
                    RefreshToken = sessionResponse.Data.RefreshToken,
                    SessionId = sessionResponse.Data.Id
                },
                Success = true,
                Message = "Token refreshed successfully."
            };
        }

        public Task<ServiceResponse<bool>> Logout(RefreshTokenRequest request)
        {
            return _sessionService.BlockSession(request.SessionId, request.RefreshToken);
        }
    }
}
