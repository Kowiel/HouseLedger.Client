using HouseLedger.Server.StaticClasses;
using HouseLedger.Server.ToolServices;
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

        [HttpGet("getalltools", Name = "GetAllTools")]
        public async Task<ActionResult<List<Tool>>> GetAll()
        {
            var tools = await _toolService.GetAllAsync();
            return Ok(tools);
        }

        [HttpGet("gettoolbyid/{id:guid}", Name = "GetToolById")]
        public async Task<ActionResult<Tool>> GetById(Guid id)
        {
            var tool = await _toolService.GetByIdAsync(id);

            if (tool is null)
            {
                return NotFound();
            }

            return Ok(tool);
        }
        [HttpPost("createtool", Name = "CreateTool")]
        public async Task<ActionResult<Tool>> Create(Tool tool)
        {
            var createdTool = await _toolService.CreateAsync(tool);
            return Ok(createdTool);
        }

        [HttpDelete("deletetool/{id:guid}", Name = "DeleteTool")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var deleted = await _toolService.DeleteAsync(id);

            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
