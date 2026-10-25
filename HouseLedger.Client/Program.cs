using MudBlazor.Services;
using HouseLedger.Client.Auth;
using HouseLedger.Client.UserService;
using ClientUserService = HouseLedger.Client.UserService.UserService;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace HouseLedger.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            var apiBaseUrl = new Uri(
                builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress,
                UriKind.Absolute);

            builder.Services.AddMudServices();
            builder.Services.AddAuthorizationCore();
            builder.Services.AddScoped<IAuthService>(_ => new AuthService(new HttpClient
            {
                BaseAddress = apiBaseUrl
            }));
            builder.Services.AddScoped<AuthMessageHandler>();
            builder.Services.AddScoped(sp =>
            {
                var authHandler = sp.GetRequiredService<AuthMessageHandler>();
                authHandler.InnerHandler = new HttpClientHandler();

                return new HttpClient(authHandler)
                {
                    BaseAddress = apiBaseUrl
                };
            });
            builder.Services.AddScoped<IUserService, ClientUserService>();
            builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthenticationStateProvider>();

            await builder.Build().RunAsync();
        }
    }
}
