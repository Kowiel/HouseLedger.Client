using System.Net.Http.Json;
using HouseLedger.Shared.DTO.Contact;
using HouseLedger.Shared.DTO.Room;
using HouseLedger.Shared.DTO.Tool;
using HouseLedger.Shared.Models;
using HouseLedger.Shared.Response;

namespace HouseLedger.Client.ToolService;

public sealed class ToolService : IToolService
{
    private const string ToolsEndpoint = "api/v1/tools";
    private readonly HttpClient _httpClient;

    public ToolService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ServiceResponse<IReadOnlyList<Tool>>> GetAllAsync()
    {
        using var response = await _httpClient.GetAsync(ToolsEndpoint);
        if (!response.IsSuccessStatusCode)
        {
            return Failure<IReadOnlyList<Tool>>(await ReadErrorMessageAsync(response, "Tools could not be loaded."));
        }

        var tools = await response.Content.ReadFromJsonAsync<List<ToolDto>>() ?? [];
        return await MapToolsAsync(tools);
    }

    public async Task<ServiceResponse<Tool?>> GetByIdAsync(Guid id)
    {
        using var response = await _httpClient.GetAsync($"{ToolsEndpoint}/{id}");
        if (!response.IsSuccessStatusCode)
        {
            return Failure<Tool?>(await ReadErrorMessageAsync(response, "Tool could not be loaded."));
        }

        var tool = await response.Content.ReadFromJsonAsync<ToolDto>();
        if (tool is null)
        {
            return Failure<Tool?>("Tool could not be loaded.");
        }

        var result = await MapToolsAsync([tool]);
        if (!result.Success)
        {
            return Failure<Tool?>(result.Message);
        }

        return new ServiceResponse<Tool?>
        {
            Data = result.Data.FirstOrDefault(),
            Success = result.Success,
            Message = result.Message
        };
    }

    public Task<ServiceResponse<Tool?>> CreateAsync(ToolRequest request) =>
        SaveAsync(_httpClient.PostAsJsonAsync(ToolsEndpoint, request), "Tool created successfully.", "Tool creation failed.");

    public Task<ServiceResponse<Tool?>> UpdateAsync(Guid id, ToolRequest request) =>
        SaveAsync(_httpClient.PutAsJsonAsync($"{ToolsEndpoint}/{id}", request), "Tool updated successfully.", "Tool update failed.");

    public async Task<ServiceResponse<bool>> DeleteAsync(Guid id)
    {
        using var response = await _httpClient.DeleteAsync($"{ToolsEndpoint}/{id}");
        if (!response.IsSuccessStatusCode)
        {
            return Failure<bool>(await ReadErrorMessageAsync(response, "Tool deletion failed."), false);
        }

        return new ServiceResponse<bool>
        {
            Data = true,
            Success = true,
            Message = "Tool deleted successfully."
        };
    }

    private async Task<ServiceResponse<Tool?>> SaveAsync(
        Task<HttpResponseMessage> responseTask,
        string successMessage,
        string failureMessage)
    {
        using var response = await responseTask;
        if (!response.IsSuccessStatusCode)
        {
            return Failure<Tool?>(await ReadErrorMessageAsync(response, failureMessage));
        }

        var dto = await response.Content.ReadFromJsonAsync<ToolDto>();
        if (dto is null)
        {
            return Failure<Tool?>(failureMessage);
        }

        var result = await MapToolsAsync([dto]);
        if (!result.Success)
        {
            return Failure<Tool?>(result.Message);
        }

        return new ServiceResponse<Tool?>
        {
            Data = result.Data.FirstOrDefault(),
            Success = true,
            Message = successMessage
        };
    }

    private async Task<ServiceResponse<IReadOnlyList<Tool>>> MapToolsAsync(IReadOnlyList<ToolDto> tools)
    {
        var roomIds = tools.Where(tool => tool.RoomId.HasValue).Select(tool => tool.RoomId!.Value).Distinct().ToArray();
        var contactIds = tools.Where(tool => tool.LentToContactId.HasValue).Select(tool => tool.LentToContactId!.Value).Distinct().ToArray();
        var rooms = new List<RoomDto>();
        var contacts = new List<ContactDto>();

        if (roomIds.Length > 0)
        {
            using var response = await _httpClient.GetAsync("api/v1/rooms");
            if (!response.IsSuccessStatusCode)
            {
                return Failure<IReadOnlyList<Tool>>(await ReadErrorMessageAsync(response, "Tool rooms could not be loaded."));
            }

            rooms = await response.Content.ReadFromJsonAsync<List<RoomDto>>() ?? [];
        }

        if (contactIds.Length > 0)
        {
            using var response = await _httpClient.GetAsync("api/v1/contacts");
            if (!response.IsSuccessStatusCode)
            {
                return Failure<IReadOnlyList<Tool>>(await ReadErrorMessageAsync(response, "Tool contacts could not be loaded."));
            }

            contacts = await response.Content.ReadFromJsonAsync<List<ContactDto>>() ?? [];
        }

        var roomsById = rooms.ToDictionary(room => room.Id);
        var contactsById = contacts.ToDictionary(contact => contact.Id);
        return new ServiceResponse<IReadOnlyList<Tool>>
        {
            Data = tools.Select(dto => MapTool(
                dto,
                dto.RoomId is Guid roomId && roomsById.TryGetValue(roomId, out var room)
                    ? new Room { Id = room.Id, Name = room.Name }
                    : null,
                dto.LentToContactId is Guid contactId && contactsById.TryGetValue(contactId, out var contact)
                    ? new Contact { Id = contact.Id, DisplayName = contact.DisplayName }
                    : null)).ToList(),
            Success = true,
            Message = "Tools loaded successfully."
        };
    }

    private static Tool MapTool(ToolDto dto, Room? room, Contact? contact) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Description = dto.Description,
        Brand = dto.Brand,
        Model = dto.Model,
        SerialNumber = dto.SerialNumber,
        Notes = dto.Notes,
        RoomId = dto.RoomId,
        Room = room,
        LentToContactId = dto.LentToContactId,
        LentToContact = contact,
        LentAt = dto.LentAt,
        CreatedAt = dto.CreatedAt
    };

    private static ServiceResponse<T> Failure<T>(string message, T data = default!) => new()
    {
        Data = data,
        Success = false,
        Message = message
    };

    private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response, string fallback)
    {
        var message = await response.Content.ReadAsStringAsync();
        return string.IsNullOrWhiteSpace(message) ? fallback : message.Trim('"');
    }
}
