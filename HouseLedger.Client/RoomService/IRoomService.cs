using HouseLedger.Shared.DTO.Room;
using HouseLedger.Shared.Models;
using HouseLedger.Shared.Response;

namespace HouseLedger.Client.RoomService;

public interface IRoomService
{
    Task<ServiceResponse<IReadOnlyList<Room>>> GetAllAsync();
    Task<ServiceResponse<Room?>> GetByIdAsync(Guid id);
    Task<ServiceResponse<Room?>> CreateAsync(RoomRequest request);
    Task<ServiceResponse<Room?>> UpdateAsync(Guid id, RoomRequest request);
    Task<ServiceResponse<bool>> DeleteAsync(Guid id);
}
