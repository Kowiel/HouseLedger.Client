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
        private static readonly TimeSpan InactivityTimeout = TimeSpan.FromMinutes(10);
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

        public async Task<bool> ValidateAccessTokenSession(Guid sessionId)
        {
            var now = DateTime.UtcNow;
            var inactivityCutoff = now.Subtract(InactivityTimeout);

            var updated = await _sessionDb.UserSessions
                .Where(session =>
                    session.Id == sessionId &&
                    !session.IsRevoked &&
                    session.ExpiresAtUtc > now &&
                    session.LastActivityAtUtc > inactivityCutoff)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(session => session.LastActivityAtUtc, now));

            return updated == 1;
        }

        // Will require refactoring to use a stored procedure for the rotation to avoid race conditions. For now, this is a simple implementation.
        public async Task<ServiceResponse<UserSession>> RotateRefreshToken(Guid sessionId, string refreshToken)
        {
            if (sessionId == Guid.Empty || string.IsNullOrWhiteSpace(refreshToken))
            {
                return RotationFailure("A valid session ID and refresh token are required.");
            }

            var now = DateTime.UtcNow;
            var newRefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var newRefreshTokenHash = HashToken(newRefreshToken);
            var oldRefreshTokenHash = HashToken(refreshToken);
            var userId = await _sessionDb.UserSessions
                .Where(session => session.Id == sessionId)
                .Select(session => (Guid?)session.UserId)
                .SingleOrDefaultAsync();

            if (userId is null)
            {
                return RotationFailure("The session could not be found.");
            }

            var updated = await _sessionDb.UserSessions
                .Where(session =>
                    session.Id == sessionId &&
                    !session.IsRevoked &&
                    session.ExpiresAtUtc > now &&
                    session.LastActivityAtUtc > now.Subtract(InactivityTimeout) &&
                    session.RefreshTokenHash == oldRefreshTokenHash)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(session => session.RefreshTokenHash, newRefreshTokenHash)
                    .SetProperty(session => session.LastActivityAtUtc, now));

            if (updated != 1)
            {
                return RotationFailure("The session is invalid, expired, revoked, or the refresh token has already been used.");
            }

            return new ServiceResponse<UserSession>
            {
                Data = new UserSession
                {
                    Id = sessionId,
                    UserId = userId.Value,
                    RefreshToken = newRefreshToken,
                    RefreshTokenHash = newRefreshTokenHash,
                    LastActivityAtUtc = now
                },
                Success = true,
                Message = "Refresh token rotated successfully."
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
            await _sessionDb.UserSessions
                .Where(session =>
                    session.UserId == user.Id &&
                    !session.IsRevoked &&
                    session.ExpiresAtUtc > now)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(session => session.IsRevoked, true));

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

            var now = DateTime.UtcNow;
            if (session.IsRevoked || session.ExpiresAtUtc <= now || session.LastActivityAtUtc <= now.Subtract(InactivityTimeout))
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

        private static ServiceResponse<UserSession> RotationFailure(string message)
        {
            return new ServiceResponse<UserSession>
            {
                Data = null,
                Success = false,
                Message = message
            };
        }
    }
}
