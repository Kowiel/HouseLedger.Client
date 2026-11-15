using HouseLedger.Shared.DTO.Auth;

namespace HouseLedger.Client.Auth;

public interface IAuthStorageService : IAsyncDisposable
{
    // Registers the callback used to fan out token changes coming from JS storage events.
    Task<TokenResponse?> InitializeAsync(Func<TokenResponse?, Task> authenticationChanged);
    Task StoreTokenAsync(TokenResponse token);
    Task ClearOwnedAuthenticationAsync();
    Task<bool> IsCurrentTabOwnerAsync();
    Task OnAuthStorageChanged(string? storedAuthentication);
}
