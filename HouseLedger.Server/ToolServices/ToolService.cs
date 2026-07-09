using HouseLedger.Server.Data;
using HouseLedger.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace HouseLedger.Server.ToolServices
{
    public class ToolService : IToolService
    {
        private readonly HouseLedgerDbContext _db;

        public ToolService(HouseLedgerDbContext db)
        {
            _db = db;
        }
        public async Task<Tool> CreateAsync(Tool tool)
        {
            tool.Id = Guid.NewGuid();
            tool.CreatedAt = DateTime.UtcNow;

            _db.Tools.Add(tool);
            await _db.SaveChangesAsync();

            return tool;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var tool = await _db.Tools.FindAsync(id);

            if (tool is null)
            {
                return false;
            }

            _db.Tools.Remove(tool);
            await _db.SaveChangesAsync();

            return true;
        }

        public async Task<List<Tool>> GetAllAsync()
        {
            return await _db.Tools
                .Include(t => t.Room)
                .Include(t => t.LentToContact)
                .ToListAsync();
        }

        public async Task<Tool?> GetByIdAsync(Guid id)
        {
            return await _db.Tools
                .Include(t => t.Room)
                .Include(t => t.LentToContact)
                .FirstOrDefaultAsync(t => t.Id == id);
        }
    }
}
