namespace HouseLedger.Shared.DTO.Room
{
    public class RoomDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }

        public static RoomDto FromEntity(Models.Room room)
        {
            return new RoomDto
            {
                Id = room.Id,
                Name = room.Name,
                Description = room.Description,
                CreatedAt = room.CreatedAt
            };
        }
    }
}