using System.ComponentModel.DataAnnotations;

namespace HouseLedger.Shared.DTO.Room
{
    public class RoomRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}