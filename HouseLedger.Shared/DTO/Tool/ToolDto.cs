namespace HouseLedger.Shared.DTO.Tool
{
    public class ToolDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string? SerialNumber { get; set; }
        public string? Notes { get; set; }
        public Guid? RoomId { get; set; }
        public Guid? LentToContactId { get; set; }
        public DateTime? LentAt { get; set; }
        public DateTime CreatedAt { get; set; }

        public static ToolDto FromEntity(Models.Tool tool)
        {
            return new ToolDto
            {
                Id = tool.Id,
                Name = tool.Name,
                Description = tool.Description,
                Brand = tool.Brand,
                Model = tool.Model,
                SerialNumber = tool.SerialNumber,
                Notes = tool.Notes,
                RoomId = tool.RoomId,
                LentToContactId = tool.LentToContactId,
                LentAt = tool.LentAt,
                CreatedAt = tool.CreatedAt
            };
        }
    }
}