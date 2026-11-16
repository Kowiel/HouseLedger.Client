using MudBlazor.Services;
using HouseLedger.Client.Auth;
using HouseLedger.Client.UserService;
using ClientUserService = HouseLedger.Client.UserService.UserService;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;

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

            ConfigureServices(builder, apiBaseUrl);

            await builder.Build().RunAsync();
        }

        private static void ConfigureServices(
            WebAssemblyHostBuilder builder,
            Uri apiBaseUrl)
        {
            builder.Services.AddMudServices();
            builder.Services.AddAuthorizationCore();
            builder.Services.AddScoped<IAuthStorageService, AuthStorageService>();
            builder.Services.AddScoped<IForcedLogoutMessageStore, ForcedLogoutMessageStore>();

            ConfigureRegistration(builder, apiBaseUrl);
            ConfigureSession(builder);
            ConfigureAuthentication(builder, apiBaseUrl);
            // App domain clients use this pipeline so normal API calls always go through token handling.
            ConfigureAuthenticatedClient(builder, apiBaseUrl);

            builder.Services.AddScoped<IUserService, ClientUserService>();
            builder.Services.AddScoped<DisplaySettingsState>();
            builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthenticationStateProvider>();
        }

        private static void ConfigureRegistration(
            WebAssemblyHostBuilder builder,
            Uri apiBaseUrl)
        {
            // Registration uses a separate client without the authentication handler.
            builder.Services.AddScoped<IRegistrationService>(_ => new RegistrationService(new HttpClient
            {
                BaseAddress = apiBaseUrl
            }));
        }

        private static void ConfigureSession(WebAssemblyHostBuilder builder)
        {
            var inactivityTimeoutMinutes =
                builder.Configuration.GetValue("Session:InactivityTimeoutMinutes", 10d);
            var warningBeforeLogoutMinutes =
                builder.Configuration.GetValue("Session:WarningBeforeLogoutMinutes", 2d);

            builder.Services.AddScoped<IInactivityService>(serviceProvider => new InactivityService(
                serviceProvider.GetRequiredService<IJSRuntime>(),
                TimeSpan.FromMinutes(inactivityTimeoutMinutes),
                TimeSpan.FromMinutes(warningBeforeLogoutMinutes)));

            builder.Services.AddScoped<ISessionService, SessionService>(serviceProvider => new SessionService(
                serviceProvider.GetRequiredService<IInactivityService>(),
                serviceProvider.GetRequiredService<NavigationManager>(),
                serviceProvider.GetRequiredService<IAuthService>(),
                serviceProvider.GetRequiredService<IForcedLogoutMessageStore>()));
        }

        private static void ConfigureAuthentication(
            WebAssemblyHostBuilder builder,
            Uri apiBaseUrl)
        {
            // Authentication also uses a separate client without the handler.
            // This avoids recursive refresh/login calls through AuthMessageHandler.
            builder.Services.AddScoped<IAuthService>(serviceProvider => new AuthService(
                new HttpClient
                {
                    BaseAddress = apiBaseUrl
                },
                serviceProvider.GetRequiredService<IAuthStorageService>(),
                serviceProvider.GetRequiredService<IInactivityService>(),
                serviceProvider.GetRequiredService<IForcedLogoutMessageStore>()));
        }

        private static void ConfigureAuthenticatedClient(
            WebAssemblyHostBuilder builder,
            Uri apiBaseUrl)
        {
            // Keep the handler scoped so it shares scoped auth/session services per Blazor circuit.
            builder.Services.AddScoped<AuthMessageHandler>();
            builder.Services.AddScoped(serviceProvider =>
            {
                var authHandler = serviceProvider.GetRequiredService<AuthMessageHandler>();
                authHandler.InnerHandler = new HttpClientHandler();

                return new HttpClient(authHandler)
                {
                    BaseAddress = apiBaseUrl
                };
            });
        }
    }
}
