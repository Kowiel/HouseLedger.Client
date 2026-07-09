using HouseLedger.Shared.Models;

namespace HouseLedger.Server.ToolServices
{
    public interface IToolService
    {
        Task<List<Tool>> GetAllAsync();
        Task<Tool?> GetByIdAsync(Guid id);
        Task<Tool> CreateAsync(Tool tool);
        Task<bool> DeleteAsync(Guid id);
    }
}
