namespace HouseLedger.Client.Auth;

public interface IInactivityService : IAsyncDisposable
{
    // Raised from JS when the warning threshold is reached.
    event Action? SessionExpiring;
    // Async pipeline for timeout side effects (logout, navigation, persistence cleanup).
    event Func<Task>? InactivityTimedOut;

    Task SetActiveAsync(bool active);
    Task StopAsync();
    ValueTask ConfirmActivityAsync();
}
