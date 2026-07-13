using System.ComponentModel.DataAnnotations;

namespace HouseLedger.Shared.DTO
{
    public class UpdateUserRequest
    {

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [MinLength(4), MaxLength(100)]
        public string? DisplayName { get; set; }

        [Phone]
        public string? PhoneNumber { get; set; }
    }
}