using System;
using System.Collections.Generic;
using System.Text;

namespace HouseLedger.Shared.Models
{
    public class Room
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = null!;
        public string? Description { get; set; }

        public Guid ApplicationUserId { get; set; }
        public AppUser ApplicationUser { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Tool> Tools { get; set; } = new List<Tool>();
    }
}
