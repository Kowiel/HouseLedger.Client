namespace HouseLedger.Client.Auth;

public interface IForcedLogoutMessageStore
{
    void SetForcedLogoutMessage(string? message);
    string? ConsumeForcedLogoutMessage();
}
