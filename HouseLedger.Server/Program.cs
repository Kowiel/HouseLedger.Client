using HouseLedger.Server.Data;
using HouseLedger.Server.Token_Sesion_Service;
using HouseLedger.Server.ToolServices;
using HouseLedger.Server.UserService;
using HouseLedger.Shared.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace HouseLedger.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var envFile = builder.Environment.IsDevelopment()
                ? ".env.development"
                : ".env";

            builder.Configuration.AddInMemoryCollection(ReadEnvFile(envFile));

            builder.Services.AddControllers();

            // Configure database connection strings from environment variables

            var dbHost = builder.Configuration["POSTGRES:HOST"];
            var dbPort = builder.Configuration["POSTGRES:PORT"];
            var dbName = builder.Configuration["POSTGRES:DB"]; 
            var dbUser = builder.Configuration["POSTGRES:USER"]; 
            var dbPassword = builder.Configuration["POSTGRES:PASSWORD"];

            var sessionDbHost = builder.Configuration["POSTGRES_SESSION:HOST"];
            var sessionDbPort = builder.Configuration["POSTGRES_SESSION:PORT"];
            var sessionDbName = builder.Configuration["POSTGRES_SESSION:DB"];
            var sessionDbUser = builder.Configuration["POSTGRES_SESSION:USER"];
            var sessionDbPassword = builder.Configuration["POSTGRES_SESSION:PASSWORD"];

            if (string.IsNullOrWhiteSpace(dbPassword))
            {
                throw new InvalidOperationException(
                    $"Database password is missing. Environment: {builder.Environment.EnvironmentName}. File tried: {envFile}");
            }

            if (string.IsNullOrWhiteSpace(sessionDbPassword))
            {
                throw new InvalidOperationException(
                    $"Session database password is missing. Environment: {builder.Environment.EnvironmentName}. File tried: {envFile}");
            }


            var connectionString =
                $"Host={dbHost};Port={dbPort};Database={dbName};UserName={dbUser};Password={dbPassword}";

            var sessionConnectionString =
                $"Host={sessionDbHost};Port={sessionDbPort};Database={sessionDbName};Username={sessionDbUser};Password={sessionDbPassword}";

            // The Database Contexts are registered with the dependency 
            builder.Services.AddDbContext<HouseLedgerDbContext>(options =>
                options.UseNpgsql(connectionString));

            builder.Services.AddDbContext<SessionDbContext>(options =>
                options.UseNpgsql(sessionConnectionString));

            // Configure JwtOptions from appsettings.json and environment variables

            builder.Services
                .AddOptions<JwtOptions>()
                .Bind(builder.Configuration.GetRequiredSection("Jwt"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // Configure JWT authentication

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    var jwt = builder.Configuration
                        .GetRequiredSection("Jwt")
                        .Get<JwtOptions>()!;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidIssuer = jwt.Issuer,
                        ValidAudience = jwt.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwt.SigningKey)),
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };
                });


            //Register services

            builder.Services.AddScoped<IToolService, ToolService>();
            builder.Services.AddScoped<IUserCRUDService, UserCRUDService>();
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<ISessionService, SessionService>();


            builder.Services
                .AddIdentityCore<AppUser>()
                .AddRoles<IdentityRole<Guid>>()
                .AddEntityFrameworkStores<HouseLedgerDbContext>();

            builder.Services.AddAuthorization();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowBlazorClient", policy =>
                {
                    policy.WithOrigins("https://localhost:7006")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseCors("AllowBlazorClient");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }

        private static Dictionary<string, string?> ReadEnvFile(string envFile)
        {
            var currentDirectory = Directory.GetCurrentDirectory();

            var possiblePaths = new[]
            {
    Path.Combine(currentDirectory, envFile),
    Path.Combine(currentDirectory, ".env", envFile),

    Path.Combine(currentDirectory, "..", envFile),
    Path.Combine(currentDirectory, "..", ".env", envFile),

    Path.Combine(currentDirectory, "..", "..", envFile),
    Path.Combine(currentDirectory, "..", "..", ".env", envFile)
};
            var path = possiblePaths.FirstOrDefault(File.Exists);

            if (path is null)
            {
                throw new FileNotFoundException($"Could not find env file: {envFile}");
            }

            var values = new Dictionary<string, string?>();

            foreach (var line in File.ReadAllLines(path))
            {
                var trimmed = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
                {
                    continue;
                }

                var separatorIndex = trimmed.IndexOf('=');

                if (separatorIndex <= 0)
                {
                    continue;
                }

                var key = trimmed[..separatorIndex]
                    .Trim()
                    .Replace("__", ":");

                var value = trimmed[(separatorIndex + 1)..]
                    .Trim()
                    .Trim('"');

                values[key] = value;
            }

            return values;
        }
    }
    }
