using System.ComponentModel.DataAnnotations;

namespace HouseLedger.Shared.DTO.Tool
{
    public class ToolRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string? SerialNumber { get; set; }
        public string? Notes { get; set; }
        public Guid? RoomId { get; set; }
        public Guid? LentToContactId { get; set; }
        public DateTime? LentAt { get; set; }
    }
}