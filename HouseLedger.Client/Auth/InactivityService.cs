using Microsoft.JSInterop;

namespace HouseLedger.Client.Auth;

public sealed class InactivityService : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly TimeSpan _inactivityTimeout;
    private readonly TimeSpan _warningBeforeLogout;
    private DotNetObjectReference<InactivityService>? _callbackReference;
    private bool _monitorStarted;

    public event Action? SessionExpiring;
    public event Func<Task>? InactivityTimedOut;

    public InactivityService(IJSRuntime jsRuntime, TimeSpan inactivityTimeout, TimeSpan warningBeforeLogout)
    {
        _jsRuntime = jsRuntime;
        _inactivityTimeout = inactivityTimeout;
        _warningBeforeLogout = warningBeforeLogout;
    }

    public async Task SetActiveAsync(bool active)
    {
        if (!active)
        {
            await StopAsync();
            return;
        }

        if (!_monitorStarted)
        {
            _callbackReference ??= DotNetObjectReference.Create(this);
            await _jsRuntime.InvokeVoidAsync(
                "houseLedgerInactivity.startInactivityMonitor",
                _inactivityTimeout.TotalMilliseconds,
                _warningBeforeLogout.TotalMilliseconds,
                _callbackReference);
            _monitorStarted = true;
        }
    }

    public async Task StopAsync()
    {
        if (_monitorStarted)
        {
            await _jsRuntime.InvokeVoidAsync("houseLedgerInactivity.stopInactivityMonitor");
            _monitorStarted = false;
        }
    }

    public ValueTask ConfirmActivityAsync() =>
        _jsRuntime.InvokeVoidAsync("houseLedgerInactivity.confirmActivity");

    [JSInvokable]
    public Task OnSessionExpiringAsync()
    {
        SessionExpiring?.Invoke();
        return Task.CompletedTask;
    }

    [JSInvokable]
    public async Task OnInactivityTimeoutAsync()
    {
        if (InactivityTimedOut is { } handlers)
        {
            foreach (Func<Task> handler in handlers.GetInvocationList())
            {
                await handler();
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _callbackReference?.Dispose();
    }
}
