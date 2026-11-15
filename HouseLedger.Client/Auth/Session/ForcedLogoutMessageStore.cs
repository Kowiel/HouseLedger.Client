namespace HouseLedger.Client.Auth;

public sealed class ForcedLogoutMessageStore : IForcedLogoutMessageStore
{
    private string? _forcedLogoutMessage;

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
}
