using HouseLedger.Server.Data;
using HouseLedger.Server.ToolServices;
using HouseLedger.Server.UserService;
using HouseLedger.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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

            var dbHost = builder.Configuration["POSTGRES:HOST"];
            var dbPort = builder.Configuration["POSTGRES:PORT"];
            var dbName = builder.Configuration["POSTGRES:DB"]; 
            var dbUser = builder.Configuration["POSTGRES:USER"]; 
            var dbPassword = builder.Configuration["POSTGRES:PASSWORD"];

            if (string.IsNullOrWhiteSpace(dbPassword))
            {
                throw new InvalidOperationException(
                    $"Database password is missing. Environment: {builder.Environment.EnvironmentName}. File tried: {envFile}");
            }

            var connectionString =
                $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";

            builder.Services.AddDbContext<HouseLedgerDbContext>(options =>
                options.UseNpgsql(connectionString));


            builder.Services.AddScoped<IToolService, ToolService>();
            builder.Services.AddScoped<IUserCRUDService, UserCRUDService>();






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
