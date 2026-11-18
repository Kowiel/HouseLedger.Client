using System.Net.Http.Json;
using HouseLedger.Shared.DTO.Room;
using HouseLedger.Shared.DTO.Tool;
using HouseLedger.Shared.Models;
using HouseLedger.Shared.Response;

namespace HouseLedger.Client.RoomService;

public sealed class RoomService : IRoomService
{
    private const string RoomsEndpoint = "api/v1/rooms";
    private readonly HttpClient _httpClient;

    public RoomService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ServiceResponse<IReadOnlyList<Room>>> GetAllAsync()
    {
        using var response = await _httpClient.GetAsync(RoomsEndpoint);
        if (!response.IsSuccessStatusCode)
        {
            return Failure<IReadOnlyList<Room>>(await ReadErrorMessageAsync(response, "Rooms could not be loaded."));
        }

        var rooms = await response.Content.ReadFromJsonAsync<List<RoomDto>>() ?? [];
        return await MapRoomsAsync(rooms);
    }

    public async Task<ServiceResponse<Room?>> GetByIdAsync(Guid id)
    {
        using var response = await _httpClient.GetAsync($"{RoomsEndpoint}/{id}");
        if (!response.IsSuccessStatusCode)
        {
            return Failure<Room?>(await ReadErrorMessageAsync(response, "Room could not be loaded."));
        }

        var room = await response.Content.ReadFromJsonAsync<RoomDto>();
        if (room is null)
        {
            return Failure<Room?>("Room could not be loaded.");
        }

        var result = await MapRoomsAsync([room]);
        if (!result.Success)
        {
            return Failure<Room?>(result.Message);
        }

        return new ServiceResponse<Room?>
        {
            Data = result.Data.FirstOrDefault(),
            Success = result.Success,
            Message = result.Message
        };
    }

    public Task<ServiceResponse<Room?>> CreateAsync(RoomRequest request) =>
        SaveAsync(_httpClient.PostAsJsonAsync(RoomsEndpoint, request), "Room created successfully.", "Room creation failed.");

    public Task<ServiceResponse<Room?>> UpdateAsync(Guid id, RoomRequest request) =>
        SaveAsync(_httpClient.PutAsJsonAsync($"{RoomsEndpoint}/{id}", request), "Room updated successfully.", "Room update failed.");

    public async Task<ServiceResponse<bool>> DeleteAsync(Guid id)
    {
        using var response = await _httpClient.DeleteAsync($"{RoomsEndpoint}/{id}");
        if (!response.IsSuccessStatusCode)
        {
            return Failure<bool>(await ReadErrorMessageAsync(response, "Room deletion failed."), false);
        }

        return new ServiceResponse<bool>
        {
            Data = true,
            Success = true,
            Message = "Room deleted successfully."
        };
    }

    private async Task<ServiceResponse<Room?>> SaveAsync(
        Task<HttpResponseMessage> responseTask,
        string successMessage,
        string failureMessage)
    {
        using var response = await responseTask;
        if (!response.IsSuccessStatusCode)
        {
            return Failure<Room?>(await ReadErrorMessageAsync(response, failureMessage));
        }

        var dto = await response.Content.ReadFromJsonAsync<RoomDto>();
        if (dto is null)
        {
            return Failure<Room?>(failureMessage);
        }

        return new ServiceResponse<Room?>
        {
            Data = MapRoom(dto, []),
            Success = true,
            Message = successMessage
        };
    }

    private async Task<ServiceResponse<IReadOnlyList<Room>>> MapRoomsAsync(IReadOnlyList<RoomDto> rooms)
    {
        var tools = new List<ToolDto>();
        if (rooms.Count > 0)
        {
            using var response = await _httpClient.GetAsync("api/v1/tools");
            if (!response.IsSuccessStatusCode)
            {
                return Failure<IReadOnlyList<Room>>(await ReadErrorMessageAsync(response, "Room tools could not be loaded."));
            }

            tools = await response.Content.ReadFromJsonAsync<List<ToolDto>>() ?? [];
        }

        return new ServiceResponse<IReadOnlyList<Room>>
        {
            Data = rooms.Select(room => MapRoom(
                room,
                tools.Where(tool => tool.RoomId == room.Id)
                    .Select(tool => new Tool
                    {
                        Id = tool.Id,
                        Name = tool.Name,
                        Description = tool.Description,
                        Brand = tool.Brand,
                        Model = tool.Model,
                        SerialNumber = tool.SerialNumber,
                        Notes = tool.Notes,
                        RoomId = tool.RoomId,
                        LentToContactId = tool.LentToContactId,
                        LentAt = tool.LentAt,
                        CreatedAt = tool.CreatedAt
                    })
                    .ToList())).ToList(),
            Success = true,
            Message = "Rooms loaded successfully."
        };
    }

    private static Room MapRoom(RoomDto dto, IEnumerable<Tool> tools) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Description = dto.Description,
        CreatedAt = dto.CreatedAt,
        Tools = tools.ToList()
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
