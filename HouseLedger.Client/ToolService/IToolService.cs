using HouseLedger.Shared.DTO.Tool;
using HouseLedger.Shared.Models;
using HouseLedger.Shared.Response;

namespace HouseLedger.Client.ToolService;

public interface IToolService
{
    Task<ServiceResponse<IReadOnlyList<Tool>>> GetAllAsync();
    Task<ServiceResponse<Tool?>> GetByIdAsync(Guid id);
    Task<ServiceResponse<Tool?>> CreateAsync(ToolRequest request);
    Task<ServiceResponse<Tool?>> UpdateAsync(Guid id, ToolRequest request);
    Task<ServiceResponse<bool>> DeleteAsync(Guid id);
}
