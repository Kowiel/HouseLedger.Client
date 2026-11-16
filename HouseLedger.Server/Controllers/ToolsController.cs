using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HouseLedger.Server.StaticClasses;
using HouseLedger.Server.ToolServices;
using HouseLedger.Shared.DTO.Tool;
using HouseLedger.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace HouseLedger.Server.Controllers
{
    [ApiController]
    [Route(ApiRoutes.Tools)]
    public class ToolsController: ControllerBase
    {
        private readonly IToolService _toolService;

        public ToolsController(IToolService toolService)
        {
            _toolService = toolService;
        }

        [HttpGet]
        [HttpGet("getalltools", Name = "GetAllTools")]
        public async Task<ActionResult<IReadOnlyList<ToolDto>>> GetAll()
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            return Ok(await _toolService.GetAllAsync(userId.Value));
        }

        [HttpGet("{id:guid}")]
        [HttpGet("gettoolbyid/{id:guid}", Name = "GetToolById")]
        public async Task<ActionResult<ToolDto>> GetById(Guid id)
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            var tool = await _toolService.GetByIdAsync(userId.Value, id);
            return tool is null ? NotFound() : Ok(tool);
        }

        [HttpPost]
        [HttpPost("createtool", Name = "CreateTool")]
        public async Task<ActionResult<ToolDto>> Create([FromBody] ToolRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            var result = await _toolService.CreateAsync(userId.Value, request);
            if (result.Status != ToolWriteStatus.Success) return WriteError(result.Status);

            return CreatedAtRoute("GetToolById", new { id = result.Tool!.Id }, result.Tool);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ToolDto>> Update(Guid id, [FromBody] ToolRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            var result = await _toolService.UpdateAsync(userId.Value, id, request);
            if (result.Status != ToolWriteStatus.Success) return WriteError(result.Status);

            return Ok(result.Tool);
        }

        [HttpDelete("{id:guid}")]
        [HttpDelete("deletetool/{id:guid}", Name = "DeleteTool")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            return await _toolService.DeleteAsync(userId.Value, id) ? NoContent() : NotFound();
        }

        private ActionResult WriteError(ToolWriteStatus status)
        {
            return status switch
            {
                ToolWriteStatus.NotFound => NotFound(),
                ToolWriteStatus.InvalidRoom => BadRequest("Room not found."),
                ToolWriteStatus.InvalidContact => BadRequest("Contact not found."),
                _ => BadRequest("A tool name is required.")
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
