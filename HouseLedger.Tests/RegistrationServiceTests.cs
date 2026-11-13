using System.Net;
using System.Net.Http.Json;
using HouseLedger.Client.Auth;
using HouseLedger.Shared.DTO.User;

namespace HouseLedger.Tests;

public class RegistrationServiceTests
{
    [Theory]
    [InlineData(true, "Account created successfully.")]
    [InlineData(false, "The account could not be created.")]
    public async Task RegisterAsync_ReturnsTheServerResult(bool created, string expectedMessage)
    {
        var handler = new RegistrationHandler(HttpStatusCode.OK, JsonContent.Create(created));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.example/") };
        var service = new RegistrationService(client);
        var request = new CreateUserRequest { UserName = "alice", Email = "alice@example.com" };

        var result = await service.RegisterAsync(request);

        Assert.Equal(created, result.Success);
        Assert.Equal(created, result.Data);
        Assert.Equal(expectedMessage, result.Message);
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("/api/v1/user/crud/createuser", handler.Path);
        Assert.Null(handler.Authorization);
        Assert.Equal(request.UserName, handler.Request?.UserName);
        Assert.Equal(request.Email, handler.Request?.Email);
    }

    [Theory]
    [InlineData("\"Account already exists.\"", "Account already exists.")]
    [InlineData("", "Registration failed.")]
    public async Task RegisterAsync_ReturnsTheErrorOrFallback(string body, string expectedMessage)
    {
        var handler = new RegistrationHandler(HttpStatusCode.BadRequest, new StringContent(body));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.example/") };
        var service = new RegistrationService(client);

        var result = await service.RegisterAsync(new CreateUserRequest());

        Assert.False(result.Success);
        Assert.False(result.Data);
        Assert.Equal(expectedMessage, result.Message);
    }

    private sealed class RegistrationHandler(HttpStatusCode status, HttpContent responseContent) : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }
        public string? Path { get; private set; }
        public string? Authorization { get; private set; }
        public CreateUserRequest? Request { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            Path = request.RequestUri?.AbsolutePath;
            Authorization = request.Headers.Authorization?.ToString();
            Request = await request.Content!.ReadFromJsonAsync<CreateUserRequest>(cancellationToken);
            return new HttpResponseMessage(status) { Content = responseContent };
        }
    }
}
