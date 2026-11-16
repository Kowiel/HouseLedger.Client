using HouseLedger.Shared.DTO.Room;

namespace HouseLedger.Server.RoomServices
{
    public enum RoomWriteStatus
    {
        Success,
        NotFound,
        DuplicateName,
        InvalidName
    }

    public record RoomWriteResult(RoomWriteStatus Status, RoomDto? Room = null);

    public interface IRoomService
    {
        Task<IReadOnlyList<RoomDto>> GetAll(Guid userId);
        Task<RoomDto?> GetById(Guid userId, Guid roomId);
        Task<RoomWriteResult> Create(Guid userId, RoomRequest request);
        Task<RoomWriteResult> Update(Guid userId, Guid roomId, RoomRequest request);
        Task<bool> Delete(Guid userId, Guid roomId);
    }
}