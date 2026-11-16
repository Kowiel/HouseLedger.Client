using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HouseLedger.Server.Controllers;
using HouseLedger.Server.RoomServices;
using HouseLedger.Shared.DTO.Room;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HouseLedger.Tests.Rooms;

public class RoomsControllerTests
{
    [Fact]
    public void ControllerRequiresAuthorization()
    {
        Assert.NotEmpty(typeof(RoomsController).GetCustomAttributes(typeof(AuthorizeAttribute), true));
    }

    [Fact]
    public async Task MissingUserIdReturnsUnauthorizedWithoutCallingService()
    {
        var service = new FakeRoomService();
        var controller = CreateController(service, null);

        Assert.IsType<UnauthorizedResult>((await controller.GetAll()).Result);
        Assert.IsType<UnauthorizedResult>((await controller.GetById(Guid.NewGuid())).Result);
        Assert.IsType<UnauthorizedResult>((await controller.Create(new RoomRequest())).Result);
        Assert.IsType<UnauthorizedResult>((await controller.Update(Guid.NewGuid(), new RoomRequest())).Result);
        Assert.IsType<UnauthorizedResult>(await controller.Delete(Guid.NewGuid()));
        Assert.Null(service.UserId);
    }

    [Fact]
    public async Task AllOperationsUseTheAuthenticatedUserId()
    {
        var service = new FakeRoomService();
        var userId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var controller = CreateController(service, userId);

        await controller.GetAll();
        Assert.Equal(userId, service.UserId);
        await controller.GetById(roomId);
        Assert.Equal((userId, roomId), (service.UserId, service.RoomId));
        await controller.Create(new RoomRequest { Name = "Kitchen" });
        Assert.Equal(userId, service.UserId);
        await controller.Update(roomId, new RoomRequest { Name = "Kitchen" });
        Assert.Equal((userId, roomId), (service.UserId, service.RoomId));
        await controller.Delete(roomId);
        Assert.Equal((userId, roomId), (service.UserId, service.RoomId));
    }

    [Theory]
    [InlineData(RoomWriteStatus.DuplicateName, typeof(ConflictObjectResult))]
    [InlineData(RoomWriteStatus.InvalidName, typeof(BadRequestObjectResult))]
    [InlineData(RoomWriteStatus.NotFound, typeof(NotFoundResult))]
    public async Task WriteFailuresReturnExpectedStatus(RoomWriteStatus status, Type expectedType)
    {
        var service = new FakeRoomService { WriteResult = new RoomWriteResult(status) };
        var controller = CreateController(service, Guid.NewGuid());
        var request = new RoomRequest { Name = "Kitchen" };

        Assert.IsType(expectedType, (await controller.Create(request)).Result);
        Assert.IsType(expectedType, (await controller.Update(Guid.NewGuid(), request)).Result);
    }

    [Fact]
    public async Task MissingRoomReturnsNotFound()
    {
        var controller = CreateController(new FakeRoomService(), Guid.NewGuid());

        Assert.IsType<NotFoundResult>((await controller.GetById(Guid.NewGuid())).Result);
        Assert.IsType<NotFoundResult>(await controller.Delete(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateReturnsLocationForNewRoom()
    {
        var room = new RoomDto { Id = Guid.NewGuid(), Name = "Kitchen" };
        var service = new FakeRoomService { WriteResult = new RoomWriteResult(RoomWriteStatus.Success, room) };
        var controller = CreateController(service, Guid.NewGuid());

        var result = Assert.IsType<CreatedAtActionResult>((await controller.Create(new RoomRequest { Name = room.Name })).Result);
        Assert.Equal(nameof(RoomsController.GetById), result.ActionName);
        Assert.Equal(room.Id, result.RouteValues!["roomId"]);
        Assert.Same(room, result.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RoomNameIsRequired(string? name)
    {
        var request = new RoomRequest { Name = name! };
        var results = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), results, true));
    }

    private static RoomsController CreateController(FakeRoomService service, Guid? userId)
    {
        var claims = userId is null ? Array.Empty<Claim>() : new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()!) };
        return new RoomsController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) }
            }
        };
    }

    private sealed class FakeRoomService : IRoomService
    {
        public Guid? UserId { get; private set; }
        public Guid? RoomId { get; private set; }
        public RoomWriteResult WriteResult { get; set; } = new(RoomWriteStatus.InvalidName);

        public Task<IReadOnlyList<RoomDto>> GetAll(Guid userId)
        {
            UserId = userId;
            return Task.FromResult<IReadOnlyList<RoomDto>>(Array.Empty<RoomDto>());
        }

        public Task<RoomDto?> GetById(Guid userId, Guid roomId)
        {
            UserId = userId;
            RoomId = roomId;
            return Task.FromResult<RoomDto?>(null);
        }

        public Task<RoomWriteResult> Create(Guid userId, RoomRequest request)
        {
            UserId = userId;
            return Task.FromResult(WriteResult);
        }

        public Task<RoomWriteResult> Update(Guid userId, Guid roomId, RoomRequest request)
        {
            UserId = userId;
            RoomId = roomId;
            return Task.FromResult(WriteResult);
        }

        public Task<bool> Delete(Guid userId, Guid roomId)
        {
            UserId = userId;
            RoomId = roomId;
            return Task.FromResult(false);
        }
    }
}