using System.ComponentModel.DataAnnotations;

namespace HouseLedger.Shared.DTO.Auth
{
    public class RefreshTokenRequest
    {
        [Required]
        public Guid SessionId { get; set; }

        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
