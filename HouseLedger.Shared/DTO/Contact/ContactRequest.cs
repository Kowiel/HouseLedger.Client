using System.ComponentModel.DataAnnotations;

namespace HouseLedger.Shared.DTO.Contact
{
    public class ContactRequest
    {
        [Required]
        public string DisplayName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Notes { get; set; }
    }
}