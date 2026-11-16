using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HouseLedger.Server.RoomServices;
using HouseLedger.Server.StaticClasses;
using HouseLedger.Shared.DTO.Room;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HouseLedger.Server.Controllers
{
    [ApiController]
    [Route(ApiRoutes.Rooms)]
    [Authorize]
    public class RoomsController : ControllerBase
    {
        private readonly IRoomService _roomService;

        public RoomsController(IRoomService roomService)
        {
            _roomService = roomService;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<RoomDto>>> GetAll()
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            return Ok(await _roomService.GetAll(userId.Value));
        }

        [HttpGet("{roomId:guid}")]
        public async Task<ActionResult<RoomDto>> GetById(Guid roomId)
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            var room = await _roomService.GetById(userId.Value, roomId);
            return room is null ? NotFound() : Ok(room);
        }

        [HttpPost]
        public async Task<ActionResult<RoomDto>> Create([FromBody] RoomRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            var result = await _roomService.Create(userId.Value, request);
            if (result.Status != RoomWriteStatus.Success) return WriteError(result.Status);

            return CreatedAtAction(nameof(GetById), new { roomId = result.Room!.Id }, result.Room);
        }

        [HttpPut("{roomId:guid}")]
        public async Task<ActionResult<RoomDto>> Update(Guid roomId, [FromBody] RoomRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            var result = await _roomService.Update(userId.Value, roomId, request);
            if (result.Status != RoomWriteStatus.Success) return WriteError(result.Status);

            return Ok(result.Room);
        }

        [HttpDelete("{roomId:guid}")]
        public async Task<IActionResult> Delete(Guid roomId)
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            return await _roomService.Delete(userId.Value, roomId) ? NoContent() : NotFound();
        }

        private ActionResult WriteError(RoomWriteStatus status)
        {
            return status switch
            {
                RoomWriteStatus.NotFound => NotFound(),
                RoomWriteStatus.DuplicateName => Conflict("A room with the same name already exists."),
                _ => BadRequest("A room name is required.")
            };
        }

        private Guid? GetCurrentUserId()
        {
            var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return Guid.TryParse(claimValue, out var userId) ? userId : null;
        }
    }
}