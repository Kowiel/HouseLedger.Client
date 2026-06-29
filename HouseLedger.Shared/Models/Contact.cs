namespace HouseLedger.Shared.Models
{
    public class Contact
    {
        public Guid Id { get; set; }

        public string DisplayName { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Notes { get; set; }

        public Guid ApplicationUserId { get; set; }
        public AppUser ApplicationUser { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Tool> BorrowedTools { get; set; } = new List<Tool>();
    }
}