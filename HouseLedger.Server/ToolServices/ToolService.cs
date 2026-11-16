using HouseLedger.Server.Data;
using HouseLedger.Shared.DTO.Tool;
using Microsoft.EntityFrameworkCore;
using ToolEntity = HouseLedger.Shared.Models.Tool;

namespace HouseLedger.Server.ToolServices
{
    public class ToolService : IToolService
    {
        private readonly HouseLedgerDbContext _db;

        public ToolService(HouseLedgerDbContext db)
        {
            _db = db;
        }
        public async Task<IReadOnlyList<ToolDto>> GetAllAsync(Guid userId)
        {
            var tools = await _db.Tools.AsNoTracking()
                .Where(tool => tool.ApplicationUserId == userId)
                .OrderBy(tool => tool.Name)
                .ToListAsync();
            return tools.Select(ToolDto.FromEntity).ToList();
        }

        public async Task<ToolDto?> GetByIdAsync(Guid userId, Guid id)
        {
            var tool = await _db.Tools.AsNoTracking()
                .SingleOrDefaultAsync(tool => tool.Id == id && tool.ApplicationUserId == userId);
            return tool is null ? null : ToolDto.FromEntity(tool);
        }

        public async Task<ToolWriteResult> CreateAsync(Guid userId, ToolRequest request)
        {
            var status = await ValidateRequest(userId, request);
            if (status != ToolWriteStatus.Success)
            {
                return new ToolWriteResult(status);
            }

            var tool = new ToolEntity { ApplicationUserId = userId };
            ApplyRequest(tool, request);
            _db.Tools.Add(tool);
            await _db.SaveChangesAsync();
            return new ToolWriteResult(ToolWriteStatus.Success, ToolDto.FromEntity(tool));
        }

        public async Task<ToolWriteResult> UpdateAsync(Guid userId, Guid id, ToolRequest request)
        {
            var tool = await _db.Tools
                .SingleOrDefaultAsync(tool => tool.Id == id && tool.ApplicationUserId == userId);
            if (tool is null)
            {
                return new ToolWriteResult(ToolWriteStatus.NotFound);
            }

            var status = await ValidateRequest(userId, request);
            if (status != ToolWriteStatus.Success)
            {
                return new ToolWriteResult(status);
            }

            ApplyRequest(tool, request);
            await _db.SaveChangesAsync();
            return new ToolWriteResult(ToolWriteStatus.Success, ToolDto.FromEntity(tool));
        }

        public async Task<bool> DeleteAsync(Guid userId, Guid id)
        {
            var tool = await _db.Tools
                .SingleOrDefaultAsync(tool => tool.Id == id && tool.ApplicationUserId == userId);

            if (tool is null)
            {
                return false;
            }

            _db.Tools.Remove(tool);
            await _db.SaveChangesAsync();

            return true;
        }

        private async Task<ToolWriteStatus> ValidateRequest(Guid userId, ToolRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return ToolWriteStatus.InvalidName;
            }

            if (request.RoomId.HasValue && !await _db.Rooms.AnyAsync(room =>
                room.Id == request.RoomId.Value && room.ApplicationUserId == userId))
            {
                return ToolWriteStatus.InvalidRoom;
            }

            if (request.LentToContactId.HasValue && !await _db.Contacts.AnyAsync(contact =>
                contact.Id == request.LentToContactId.Value && contact.ApplicationUserId == userId))
            {
                return ToolWriteStatus.InvalidContact;
            }

            return ToolWriteStatus.Success;
        }

        private static void ApplyRequest(ToolEntity tool, ToolRequest request)
        {
            tool.Name = request.Name.Trim();
            tool.Description = request.Description;
            tool.Brand = request.Brand;
            tool.Model = request.Model;
            tool.SerialNumber = request.SerialNumber;
            tool.Notes = request.Notes;
            tool.RoomId = request.RoomId;
            tool.LentToContactId = request.LentToContactId;
            tool.LentAt = request.LentAt;
        }
    }
}
