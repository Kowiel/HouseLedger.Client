using HouseLedger.Shared.DTO.Auth;
using HouseLedger.Shared.Response;
using Microsoft.AspNetCore.Components;

namespace HouseLedger.Client.Auth;

public sealed class SessionService : ISessionService
{
    private readonly IInactivityService _inactivityService;
    private readonly NavigationManager _navigation;
    private readonly IAuthService _authService;
    private readonly IForcedLogoutMessageStore _forcedLogoutMessageStore;

    public event Action? SessionExpiring;

    public SessionService(
        IInactivityService inactivityService,
        NavigationManager navigation,
        IAuthService authService,
        IForcedLogoutMessageStore forcedLogoutMessageStore)
    {
        _inactivityService = inactivityService;
        _navigation = navigation;
        _authService = authService;
        _forcedLogoutMessageStore = forcedLogoutMessageStore;
        _inactivityService.SessionExpiring += OnSessionExpiring;
        _inactivityService.InactivityTimedOut += OnInactivityTimeoutAsync;
    }

    public Task SetActiveAsync(bool active) => _inactivityService.SetActiveAsync(active);

    public void SetForcedLogoutMessage(string? message)
    {
        _forcedLogoutMessageStore.SetForcedLogoutMessage(message);
    }

    public string? ConsumeForcedLogoutMessage() => _forcedLogoutMessageStore.ConsumeForcedLogoutMessage();

    public async Task<ServiceResponse<TokenResponse>> ConfirmSessionActivityAsync()
    {
        var response = await _authService.RefreshAsync(force: true, isActivityConfirmation: true);
        if (response.Success)
        {
            await _inactivityService.ConfirmActivityAsync();
        }

        return response;
    }

    private async Task OnInactivityTimeoutAsync()
    {
        if (_authService.CurrentToken is null)
        {
            return;
        }

        // Persist a user-facing reason before auth state is cleared and navigation occurs.
        SetForcedLogoutMessage("You were logged out due to inactivity. Please sign in again.");
        try
        {
            await _authService.LogoutAsync();
        }
        finally
        {
            await _authService.ClearAuthenticationAsync();
            _navigation.NavigateTo("/login");
        }
    }

    private void OnSessionExpiring() => SessionExpiring?.Invoke();

    public async ValueTask DisposeAsync()
    {
        _inactivityService.SessionExpiring -= OnSessionExpiring;
        _inactivityService.InactivityTimedOut -= OnInactivityTimeoutAsync;
        await _inactivityService.StopAsync();
    }
}
