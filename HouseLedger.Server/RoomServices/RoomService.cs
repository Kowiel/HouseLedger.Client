using HouseLedger.Server.Data;
using HouseLedger.Shared.DTO.Room;
using Microsoft.EntityFrameworkCore;
using RoomEntity = HouseLedger.Shared.Models.Room;

namespace HouseLedger.Server.RoomServices
{
    public class RoomService : IRoomService
    {
        private readonly HouseLedgerDbContext _dbContext;

        public RoomService(HouseLedgerDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<RoomDto>> GetAll(Guid userId)
        {
            var rooms = await _dbContext.Rooms.AsNoTracking()
                .Where(room => room.ApplicationUserId == userId)
                .OrderBy(room => room.Name)
                .ToListAsync();
            return rooms.Select(RoomDto.FromEntity).ToList();
        }

        public async Task<RoomDto?> GetById(Guid userId, Guid roomId)
        {
            var room = await _dbContext.Rooms.AsNoTracking()
                .SingleOrDefaultAsync(room => room.Id == roomId && room.ApplicationUserId == userId);
            return room is null ? null : RoomDto.FromEntity(room);
        }

        public async Task<RoomWriteResult> Create(Guid userId, RoomRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return new RoomWriteResult(RoomWriteStatus.InvalidName);
            }

            var name = request.Name.Trim();
            if (await NameExists(userId, name, null))
            {
                return new RoomWriteResult(RoomWriteStatus.DuplicateName);
            }

            var room = new RoomEntity
            {
                Name = name,
                Description = request.Description,
                ApplicationUserId = userId
            };
            _dbContext.Rooms.Add(room);
            return await SaveRoom(room);
        }

        public async Task<RoomWriteResult> Update(Guid userId, Guid roomId, RoomRequest request)
        {
            var room = await _dbContext.Rooms
                .SingleOrDefaultAsync(room => room.Id == roomId && room.ApplicationUserId == userId);
            if (room is null)
            {
                return new RoomWriteResult(RoomWriteStatus.NotFound);
            }
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return new RoomWriteResult(RoomWriteStatus.InvalidName);
            }

            var name = request.Name.Trim();
            if (await NameExists(userId, name, roomId))
            {
                return new RoomWriteResult(RoomWriteStatus.DuplicateName);
            }

            room.Name = name;
            room.Description = request.Description;
            return await SaveRoom(room);
        }

        public async Task<bool> Delete(Guid userId, Guid roomId)
        {
            var room = await _dbContext.Rooms
                .SingleOrDefaultAsync(room => room.Id == roomId && room.ApplicationUserId == userId);
            if (room is null)
            {
                return false;
            }

            _dbContext.Rooms.Remove(room);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        private Task<bool> NameExists(Guid userId, string name, Guid? excludedRoomId)
        {
            var normalizedName = name.ToLower();
            return _dbContext.Rooms.AnyAsync(room => room.ApplicationUserId == userId
                && (!excludedRoomId.HasValue || room.Id != excludedRoomId.Value)
                && room.Name.Trim().ToLower() == normalizedName);
        }

        private async Task<RoomWriteResult> SaveRoom(RoomEntity room)
        {
            await _dbContext.SaveChangesAsync();
            return new RoomWriteResult(RoomWriteStatus.Success, RoomDto.FromEntity(room));
        }
    }
}