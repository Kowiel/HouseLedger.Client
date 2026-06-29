namespace HouseLedger.Shared.Models
{
    public class Tool
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = null!;
        public string? Description { get; set; }

        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string? SerialNumber { get; set; }

        public string? Notes { get; set; }

        public Guid ApplicationUserId { get; set; }
        public AppUser ApplicationUser { get; set; } = null!;

        public Guid? RoomId { get; set; }
        public Room? Room { get; set; }

        public Guid? LentToContactId { get; set; }
        public Contact? LentToContact { get; set; }

        public DateTime? LentAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}