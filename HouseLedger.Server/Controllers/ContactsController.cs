using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HouseLedger.Server.ContactServices;
using HouseLedger.Server.StaticClasses;
using HouseLedger.Shared.DTO.Contact;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HouseLedger.Server.Controllers
{
    [ApiController]
    [Route(ApiRoutes.Contacts)]
    [Authorize]
    public class ContactsController : ControllerBase
    {
        private readonly IContactService _contactService;

        public ContactsController(IContactService contactService)
        {
            _contactService = contactService;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ContactDto>>> GetAll()
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            return Ok(await _contactService.GetAll(userId.Value));
        }

        [HttpGet("{contactId:guid}")]
        public async Task<ActionResult<ContactDto>> GetById(Guid contactId)
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            var contact = await _contactService.GetById(userId.Value, contactId);
            return contact is null ? NotFound() : Ok(contact);
        }

        [HttpPost]
        public async Task<ActionResult<ContactDto>> Create([FromBody] ContactRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            var result = await _contactService.Create(userId.Value, request);
            if (result.Status != ContactWriteStatus.Success) return WriteError(result.Status);

            return CreatedAtAction(nameof(GetById), new { contactId = result.Contact!.Id }, result.Contact);
        }

        [HttpPut("{contactId:guid}")]
        public async Task<ActionResult<ContactDto>> Update(Guid contactId, [FromBody] ContactRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            var result = await _contactService.Update(userId.Value, contactId, request);
            if (result.Status != ContactWriteStatus.Success) return WriteError(result.Status);

            return Ok(result.Contact);
        }

        [HttpDelete("{contactId:guid}")]
        public async Task<IActionResult> Delete(Guid contactId)
        {
            var userId = GetCurrentUserId();
            if (userId is null) return Unauthorized();

            var status = await _contactService.Delete(userId.Value, contactId);
            return status switch
            {
                ContactDeleteStatus.Success => NoContent(),
                ContactDeleteStatus.NotFound => NotFound(),
                _ => Conflict("Return the contact's borrowed tools before deleting the contact.")
            };
        }

        private ActionResult WriteError(ContactWriteStatus status)
        {
            return status == ContactWriteStatus.NotFound
                ? NotFound()
                : BadRequest("A contact display name is required.");
        }

        private Guid? GetCurrentUserId()
        {
            var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return Guid.TryParse(claimValue, out var userId) ? userId : null;
        }
    }
}