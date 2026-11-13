using System.Net.Http.Json;
using HouseLedger.Shared.DTO.User;
using HouseLedger.Shared.Response;

namespace HouseLedger.Client.Auth;

public sealed class RegistrationService
{
    private readonly HttpClient _httpClient;

    public RegistrationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ServiceResponse<bool>> RegisterAsync(CreateUserRequest request)
    {
        using var response = await _httpClient.PostAsJsonAsync("api/v1/user/crud/createuser", request);
        if (response.IsSuccessStatusCode)
        {
            var created = await response.Content.ReadFromJsonAsync<bool>();
            return new ServiceResponse<bool>
            {
                Data = created,
                Success = created,
                Message = created ? "Account created successfully." : "The account could not be created."
            };
        }

        var message = await response.Content.ReadAsStringAsync();
        return new ServiceResponse<bool>
        {
            Data = false,
            Success = false,
            Message = string.IsNullOrWhiteSpace(message) ? "Registration failed." : message.Trim('"')
        };
    }
}
