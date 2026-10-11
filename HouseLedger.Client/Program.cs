using MudBlazor.Services;
using HouseLedger.Client.Auth;
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
            builder.Services.AddScoped<IAuthService>(_ => new AuthService(new HttpClient
            {
                BaseAddress = apiBaseUrl
            }));
            builder.Services.AddScoped<AuthMessageHandler>();
            builder.Services.AddScoped(sp => new HttpClient(sp.GetRequiredService<AuthMessageHandler>())
            {
                BaseAddress = apiBaseUrl
            });

            await builder.Build().RunAsync();
        }
    }
}
