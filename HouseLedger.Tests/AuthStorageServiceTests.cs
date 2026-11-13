using System.Text.Json;
using HouseLedger.Client.Auth;
using HouseLedger.Shared.DTO.Auth;
using Microsoft.JSInterop;

namespace HouseLedger.Tests;

public class AuthStorageServiceTests
{
    [Theory]
    [InlineData("{\"AccessToken\":\"old-access\",\"RefreshToken\":\"old-refresh\"}")]
    [InlineData("not-json")]
    [InlineData(null)]
    public async Task Initialize_WithLegacyOnlyToken_ClearsItWithoutRestoringAuthentication(string? legacyToken)
    {
        var js = new FakeJsRuntime { LegacyToken = legacyToken };
        await using var service = new AuthStorageService(js);

        var token = await service.InitializeAsync(_ => Task.CompletedTask);
        var repeatedToken = await service.InitializeAsync(_ => Task.CompletedTask);

        Assert.Null(token);
        Assert.Null(repeatedToken);
        Assert.Null(js.LegacyToken);
        Assert.Null(js.StoredAuthentication);
        Assert.Single(js.Calls, call => call.Identifier == "sessionStorage.removeItem" &&
            Equals(call.Arguments[0], "houseLedger.tokenResponse"));
        Assert.Single(js.Calls, call => call.Identifier == "houseLedgerAuthStorage.subscribe");
        Assert.DoesNotContain(js.Calls, call => call.Identifier == "sessionStorage.getItem");
        Assert.DoesNotContain(js.Calls, call => call.Identifier == "houseLedgerAuthStorage.set");
    }

    [Fact]
    public async Task Initialize_WithCurrentTabAuthentication_RestoresItAndClearsLegacyToken()
    {
        var storedToken = CreateToken();
        var storedAuthentication = SerializeAuthentication("test-tab", storedToken);
        var js = new FakeJsRuntime { LegacyToken = "old-token", StoredAuthentication = storedAuthentication };
        await using var service = new AuthStorageService(js);

        var token = await service.InitializeAsync(_ => Task.CompletedTask);

        Assert.NotNull(token);
        Assert.Equal(storedToken.AccessToken, token.AccessToken);
        Assert.Equal(storedToken.RefreshToken, token.RefreshToken);
        Assert.Equal(storedToken.SessionId, token.SessionId);
        Assert.True(await service.IsCurrentTabOwnerAsync());
        Assert.Null(js.LegacyToken);
        Assert.Equal(storedAuthentication, js.StoredAuthentication);
        Assert.DoesNotContain(js.Calls, call => call.Identifier == "houseLedgerAuthStorage.remove");
        Assert.DoesNotContain(js.Calls, call => call.Identifier == "houseLedgerAuthStorage.set");
    }

    [Fact]
    public async Task Initialize_WithAnotherTabsAuthentication_PreservesItAndClearsLegacyToken()
    {
        var storedAuthentication = SerializeAuthentication("other-tab", CreateToken());
        var js = new FakeJsRuntime { LegacyToken = "old-token", StoredAuthentication = storedAuthentication };
        await using var service = new AuthStorageService(js);

        var token = await service.InitializeAsync(_ => Task.CompletedTask);
        var isOwner = await service.IsCurrentTabOwnerAsync();
        await service.ClearOwnedAuthenticationAsync();

        Assert.Null(token);
        Assert.False(isOwner);
        Assert.Null(js.LegacyToken);
        Assert.Equal(storedAuthentication, js.StoredAuthentication);
        Assert.DoesNotContain(js.Calls, call => call.Identifier == "houseLedgerAuthStorage.remove");
    }

    [Fact]
    public async Task Initialize_WithMalformedCurrentRecord_RemovesBothRecordsWithoutMigration()
    {
        var js = new FakeJsRuntime { LegacyToken = "old-token", StoredAuthentication = "not-json" };
        await using var service = new AuthStorageService(js);

        var token = await service.InitializeAsync(_ => Task.CompletedTask);

        Assert.Null(token);
        Assert.Null(js.LegacyToken);
        Assert.Null(js.StoredAuthentication);
        Assert.Single(js.Calls, call => call.Identifier == "houseLedgerAuthStorage.remove");
        Assert.DoesNotContain(js.Calls, call => call.Identifier == "sessionStorage.getItem");
        Assert.DoesNotContain(js.Calls, call => call.Identifier == "houseLedgerAuthStorage.set");
    }

    [Fact]
    public async Task StorageChanges_StillNotifyOnlyWithCurrentTabToken()
    {
        var js = new FakeJsRuntime { LegacyToken = "old-token" };
        await using var service = new AuthStorageService(js);
        var notifications = new List<TokenResponse?>();
        await service.InitializeAsync(token =>
        {
            notifications.Add(token);
            return Task.CompletedTask;
        });
        var currentToken = CreateToken();

        await service.OnAuthStorageChanged(SerializeAuthentication("other-tab", CreateToken()));
        await service.OnAuthStorageChanged(SerializeAuthentication("test-tab", currentToken));

        Assert.Equal(2, notifications.Count);
        Assert.Null(notifications[0]);
        Assert.Equal(currentToken.SessionId, notifications[1]?.SessionId);
        Assert.Equal(currentToken.AccessToken, notifications[1]?.AccessToken);
        Assert.Null(js.LegacyToken);
    }

    private static TokenResponse CreateToken() => new()
    {
        AccessToken = "access-token",
        RefreshToken = "refresh-token",
        SessionId = Guid.NewGuid()
    };

    private static string SerializeAuthentication(string tabId, TokenResponse token) =>
        JsonSerializer.Serialize(new { TabId = tabId, Token = token });

    private sealed class FakeJsRuntime : IJSRuntime
    {
        public string TabId { get; set; } = "test-tab";
        public string? LegacyToken { get; set; }
        public string? StoredAuthentication { get; set; }
        public List<JsCall> Calls { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            var arguments = args ?? [];
            Calls.Add(new JsCall(identifier, arguments));
            object? result = identifier switch
            {
                "houseLedgerAuthStorage.getTabId" => TabId,
                "houseLedgerAuthStorage.subscribe" => 1,
                "houseLedgerAuthStorage.get" => StoredAuthentication,
                "sessionStorage.getItem" => LegacyToken,
                _ => null
            };
            if (identifier == "sessionStorage.removeItem" && Equals(arguments[0], "houseLedger.tokenResponse"))
            {
                LegacyToken = null;
            }
            else if (identifier == "houseLedgerAuthStorage.set")
            {
                StoredAuthentication = (string?)arguments[1];
            }
            else if (identifier == "houseLedgerAuthStorage.remove")
            {
                StoredAuthentication = null;
            }

            return ValueTask.FromResult(result is null ? default! : (TValue)result);
        }
    }

    private sealed record JsCall(string Identifier, object?[] Arguments);
}
