using HouseLedger.Shared.DTO.Tool;

namespace HouseLedger.Server.ToolServices
{
    public enum ToolWriteStatus
    {
        Success,
        NotFound,
        InvalidName,
        InvalidRoom,
        InvalidContact
    }

    public record ToolWriteResult(ToolWriteStatus Status, ToolDto? Tool = null);

    public interface IToolService
    {
        Task<IReadOnlyList<ToolDto>> GetAllAsync(Guid userId);
        Task<ToolDto?> GetByIdAsync(Guid userId, Guid id);
        Task<ToolWriteResult> CreateAsync(Guid userId, ToolRequest request);
        Task<ToolWriteResult> UpdateAsync(Guid userId, Guid id, ToolRequest request);
        Task<bool> DeleteAsync(Guid userId, Guid id);
    }
}
