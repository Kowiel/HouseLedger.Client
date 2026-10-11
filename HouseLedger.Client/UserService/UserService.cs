using System.Net.Http.Json;
using HouseLedger.Shared.DTO.User;
using HouseLedger.Shared.Response;

namespace HouseLedger.Client.UserService;

public sealed class UserService : IUserService
{
    private readonly HttpClient _httpClient;

    public UserService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ServiceResponse<bool>> CreateAsync(CreateUserRequest request)
    {
        using var response = await _httpClient.PostAsJsonAsync("api/v1/user/crud/createuser", request);
        return await ReadBooleanResponseAsync(response, "User created successfully.", "User creation failed.");
    }

    public async Task<ServiceResponse<BasicUserInfo?>> GetBasicAsync()
    {
        using var response = await _httpClient.GetAsync("api/v1/user/crud/me/basic");
        if (!response.IsSuccessStatusCode)
        {
            return new ServiceResponse<BasicUserInfo?>
            {
                Success = false,
                Message = await ReadErrorMessageAsync(response, "User could not be loaded.")
            };
        }

        return new ServiceResponse<BasicUserInfo?>
        {
            Data = await response.Content.ReadFromJsonAsync<BasicUserInfo>(),
            Success = true,
            Message = "User loaded successfully."
        };
    }

    public async Task<ServiceResponse<FullUserInfo?>> GetFullAsync()
    {
        using var response = await _httpClient.GetAsync("api/v1/user/crud/me");
        if (!response.IsSuccessStatusCode)
        {
            return new ServiceResponse<FullUserInfo?>
            {
                Success = false,
                Message = await ReadErrorMessageAsync(response, "User could not be loaded.")
            };
        }

        return new ServiceResponse<FullUserInfo?>
        {
            Data = await response.Content.ReadFromJsonAsync<FullUserInfo>(),
            Success = true,
            Message = "User loaded successfully."
        };
    }

    public async Task<ServiceResponse<bool>> UpdateAsync(UpdateUserRequest request)
    {
        using var response = await _httpClient.PutAsJsonAsync("api/v1/user/crud/me", request);
        return await ReadBooleanResponseAsync(response, "User updated successfully.", "User update failed.");
    }

    public async Task<ServiceResponse<bool>> DeleteAsync()
    {
        using var response = await _httpClient.DeleteAsync("api/v1/user/crud/me");
        return await ReadBooleanResponseAsync(response, "User deleted successfully.", "User deletion failed.");
    }

    private static async Task<ServiceResponse<bool>> ReadBooleanResponseAsync(
        HttpResponseMessage response,
        string successMessage,
        string failureMessage)
    {
        if (!response.IsSuccessStatusCode)
        {
            return new ServiceResponse<bool>
            {
                Data = false,
                Success = false,
                Message = await ReadErrorMessageAsync(response, failureMessage)
            };
        }

        var result = await response.Content.ReadFromJsonAsync<bool>();
        return new ServiceResponse<bool>
        {
            Data = result,
            Success = result,
            Message = result ? successMessage : failureMessage
        };
    }

    private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response, string fallback)
    {
        var message = await response.Content.ReadAsStringAsync();
        return string.IsNullOrWhiteSpace(message) ? fallback : message.Trim('"');
    }
}
