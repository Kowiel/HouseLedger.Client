namespace HouseLedger.Shared.DTO.Contact
{
    public class ContactDto
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }

        public static ContactDto FromEntity(Models.Contact contact)
        {
            return new ContactDto
            {
                Id = contact.Id,
                DisplayName = contact.DisplayName,
                PhoneNumber = contact.PhoneNumber,
                Email = contact.Email,
                Notes = contact.Notes,
                CreatedAt = contact.CreatedAt
            };
        }
    }
}