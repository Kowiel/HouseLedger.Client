using HouseLedger.Shared.DTO.Contact;

namespace HouseLedger.Server.ContactServices
{
    public enum ContactWriteStatus
    {
        Success,
        NotFound,
        InvalidName
    }

    public enum ContactDeleteStatus
    {
        Success,
        NotFound,
        HasBorrowedTools
    }

    public record ContactWriteResult(ContactWriteStatus Status, ContactDto? Contact = null);

    public interface IContactService
    {
        Task<IReadOnlyList<ContactDto>> GetAll(Guid userId);
        Task<ContactDto?> GetById(Guid userId, Guid contactId);
        Task<ContactWriteResult> Create(Guid userId, ContactRequest request);
        Task<ContactWriteResult> Update(Guid userId, Guid contactId, ContactRequest request);
        Task<ContactDeleteStatus> Delete(Guid userId, Guid contactId);
    }
}