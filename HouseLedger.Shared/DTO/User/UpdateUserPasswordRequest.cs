using System.ComponentModel.DataAnnotations;

namespace HouseLedger.Shared.DTO.User
{
    public class UpdateUserPasswordRequest
    {
        [Required]
        [MinLength(8)]
        public string OldPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        public string RetypeNewPassword { get; set; } = string.Empty;
    }
}