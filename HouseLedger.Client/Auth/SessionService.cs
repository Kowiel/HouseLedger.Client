using HouseLedger.Shared.DTO.Auth;
using HouseLedger.Shared.Response;
using Microsoft.AspNetCore.Components;

namespace HouseLedger.Client.Auth;

public sealed class SessionService : IAsyncDisposable
{
    private readonly InactivityService _inactivityService;
    private readonly NavigationManager _navigation;
    private readonly Func<IAuthService> _getAuthService;
    private string? _forcedLogoutMessage;

    public event Action? SessionExpiring;

    public SessionService(InactivityService inactivityService, NavigationManager navigation, Func<IAuthService> getAuthService)
    {
        _inactivityService = inactivityService;
        _navigation = navigation;
        _getAuthService = getAuthService;
        _inactivityService.SessionExpiring += OnSessionExpiring;
        _inactivityService.InactivityTimedOut += OnInactivityTimeoutAsync;
    }

    public Task SetActiveAsync(bool active) => _inactivityService.SetActiveAsync(active);

    public void SetForcedLogoutMessage(string? message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            _forcedLogoutMessage ??= message;
        }
    }

    public string? ConsumeForcedLogoutMessage()
    {
        var message = _forcedLogoutMessage;
        _forcedLogoutMessage = null;
        return message;
    }

    public async Task<ServiceResponse<TokenResponse>> ConfirmSessionActivityAsync()
    {
        var response = await _getAuthService().RefreshAsync(force: true, isActivityConfirmation: true);
        if (response.Success)
        {
            await _inactivityService.ConfirmActivityAsync();
        }

        return response;
    }

    private async Task OnInactivityTimeoutAsync()
    {
        var authService = _getAuthService();
        if (authService.CurrentToken is null)
        {
            return;
        }

        SetForcedLogoutMessage("You were logged out due to inactivity. Please sign in again.");
        try
        {
            await authService.LogoutAsync();
        }
        finally
        {
            await authService.ClearAuthenticationAsync();
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
