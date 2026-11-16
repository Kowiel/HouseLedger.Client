using HouseLedger.Shared.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HouseLedger.Server.Token_Sesion_Service
{
    public class TokenService: ITokenService
    {
        private readonly JwtOptions _options;
        public TokenService(IOptions<JwtOptions> options)
        {
            _options = options.Value;
        }
        public string CreateAccessToken(AppUser user, UserSession session)
        {
            var claims = new List<Claim>
            {
                new( JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new( "sid", session.Id.ToString()),
                new( JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_options.AccessTokenLifetimeMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
