
namespace HouseLedger.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();

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

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseCors("AllowBlazorClient");

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
