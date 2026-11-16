using HouseLedger.Server.Data;
using HouseLedger.Shared.DTO.Contact;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ContactEntity = HouseLedger.Shared.Models.Contact;

namespace HouseLedger.Server.ContactServices
{
    public class ContactService : IContactService
    {
        private readonly HouseLedgerDbContext _dbContext;

        public ContactService(HouseLedgerDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<ContactDto>> GetAll(Guid userId)
        {
            var contacts = await _dbContext.Contacts.AsNoTracking()
                .Where(contact => contact.ApplicationUserId == userId)
                .OrderBy(contact => contact.DisplayName)
                .ToListAsync();
            return contacts.Select(ContactDto.FromEntity).ToList();
        }

        public async Task<ContactDto?> GetById(Guid userId, Guid contactId)
        {
            var contact = await _dbContext.Contacts.AsNoTracking()
                .SingleOrDefaultAsync(contact => contact.Id == contactId && contact.ApplicationUserId == userId);
            return contact is null ? null : ContactDto.FromEntity(contact);
        }

        public async Task<ContactWriteResult> Create(Guid userId, ContactRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.DisplayName))
            {
                return new ContactWriteResult(ContactWriteStatus.InvalidName);
            }

            var contact = new ContactEntity { ApplicationUserId = userId };
            ApplyRequest(contact, request);
            _dbContext.Contacts.Add(contact);
            await _dbContext.SaveChangesAsync();
            return new ContactWriteResult(ContactWriteStatus.Success, ContactDto.FromEntity(contact));
        }

        public async Task<ContactWriteResult> Update(Guid userId, Guid contactId, ContactRequest request)
        {
            var contact = await _dbContext.Contacts
                .SingleOrDefaultAsync(contact => contact.Id == contactId && contact.ApplicationUserId == userId);
            if (contact is null)
            {
                return new ContactWriteResult(ContactWriteStatus.NotFound);
            }
            if (string.IsNullOrWhiteSpace(request.DisplayName))
            {
                return new ContactWriteResult(ContactWriteStatus.InvalidName);
            }

            ApplyRequest(contact, request);
            await _dbContext.SaveChangesAsync();
            return new ContactWriteResult(ContactWriteStatus.Success, ContactDto.FromEntity(contact));
        }

        public async Task<ContactDeleteStatus> Delete(Guid userId, Guid contactId)
        {
            var contact = await _dbContext.Contacts
                .SingleOrDefaultAsync(contact => contact.Id == contactId && contact.ApplicationUserId == userId);
            if (contact is null)
            {
                return ContactDeleteStatus.NotFound;
            }
            if (await _dbContext.Tools.AnyAsync(tool => tool.LentToContactId == contactId))
            {
                return ContactDeleteStatus.HasBorrowedTools;
            }

            _dbContext.Contacts.Remove(contact);
            try
            {
                await _dbContext.SaveChangesAsync();
                return ContactDeleteStatus.Success;
            }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException
                { SqlState: PostgresErrorCodes.ForeignKeyViolation, ConstraintName: "FK_Tools_Contacts_LentToContactId" })
            {
                return ContactDeleteStatus.HasBorrowedTools;
            }
        }

        private static void ApplyRequest(ContactEntity contact, ContactRequest request)
        {
            contact.DisplayName = request.DisplayName.Trim();
            contact.PhoneNumber = request.PhoneNumber;
            contact.Email = request.Email;
            contact.Notes = request.Notes;
        }
    }
}