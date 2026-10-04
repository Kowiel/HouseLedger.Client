using HouseLedger.Server.Data;
using HouseLedger.Shared.DTO.User;
using HouseLedger.Shared.Models;
using HouseLedger.Shared.Response;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace HouseLedger.Server.Token_Sesion_Service
{
    public class SessionService : ISessionService
    {
        private readonly SessionDbContext _sessionDb;
        public SessionService(SessionDbContext sessionDb)
        {
            _sessionDb = sessionDb;
        }
        public async Task<ServiceResponse<bool>> BlockSession(Guid sessionId, string refreshToken)
        {
            if (sessionId == Guid.Empty || string.IsNullOrWhiteSpace(refreshToken))
            {
                return Failure("A valid session ID and refresh token are required.");
            }

            var session = await _sessionDb.UserSessions
                .SingleOrDefaultAsync(x => x.Id == sessionId);

            if (session is null || !TokenMatches(session.RefreshTokenHash, refreshToken))
            {
                return Failure("The session could not be found or the refresh token is invalid.");
            }

            if (session.IsRevoked)
            {
                return Failure("The session has already been revoked.");
            }

            session.IsRevoked = true;
            await _sessionDb.SaveChangesAsync();

            return new ServiceResponse<bool>
            {
                Data = true,
                Success = true,
                Message = "Session blocked successfully."
            };
        }

        public async Task<ServiceResponse<UserSession>> CreateSession(FullUserInfo user)
        {
            if (user is null || user.Id == Guid.Empty)
            {
                return new ServiceResponse<UserSession>
                {
                    Data = null,
                    Success = false,
                    Message = "A valid user is required to create a session."
                };
            }

            var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var now = DateTime.UtcNow;
            var userSession = new UserSession
            {
                Id = Guid.CreateVersion7(),
                UserId = user.Id,
                RefreshToken = refreshToken,
                RefreshTokenHash = HashToken(refreshToken),
                CreatedAtUtc = now,
                LastActivityAtUtc = now,
                ExpiresAtUtc = now.AddMinutes(30),
                IsRevoked = false
            };

            _sessionDb.UserSessions.Add(userSession);
            await _sessionDb.SaveChangesAsync();

            return new ServiceResponse<UserSession>
            {
                Data = userSession,
                Success = true,
                Message = "Session created successfully."
            };
        }

        public async Task<ServiceResponse<bool>> ValidateSession(Guid sessionId, string refreshToken)
        {
            if (sessionId == Guid.Empty || string.IsNullOrWhiteSpace(refreshToken))
            {
                return Failure("A valid session ID and refresh token are required.");
            }

            var session = await _sessionDb.UserSessions
                .SingleOrDefaultAsync(x => x.Id == sessionId);

            if (session is null || !TokenMatches(session.RefreshTokenHash, refreshToken))
            {
                return Failure("The session could not be found or the refresh token is invalid.");
            }

            if (session.IsRevoked || session.ExpiresAtUtc <= DateTime.UtcNow)
            {
                return Failure("The session is revoked or expired.");
            }

            session.LastActivityAtUtc = DateTime.UtcNow;
            await _sessionDb.SaveChangesAsync();

            return new ServiceResponse<bool>
            {
                Data = true,
                Success = true,
                Message = "Session is valid."
            };
        }

        private string HashToken(string token)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(token);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        private bool TokenMatches(string storedHash, string refreshToken)
        {
            var expectedHash = Convert.FromBase64String(storedHash);
            var actualHash = Convert.FromBase64String(HashToken(refreshToken));
            return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
        }

        private static ServiceResponse<bool> Failure(string message)
        {
            return new ServiceResponse<bool>
            {
                Data = false,
                Success = false,
                Message = message
            };
        }
    }
}
