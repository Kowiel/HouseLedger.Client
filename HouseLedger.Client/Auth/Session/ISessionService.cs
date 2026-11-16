using HouseLedger.Shared.DTO.Auth;
using HouseLedger.Shared.Response;

namespace HouseLedger.Client.Auth;

public interface ISessionService : IAsyncDisposable
{
    // Raised when inactivity monitor requests a user confirmation prompt.
    event Action? SessionExpiring;

    string? ConsumeForcedLogoutMessage();
    Task<ServiceResponse<TokenResponse>> ConfirmSessionActivityAsync();
}
