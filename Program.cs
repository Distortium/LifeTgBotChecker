using LifeTgBotChecker.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LifeTgBotChecker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();
            builder.Services.AddSingleton<LifeCheckBotsService>();

            builder.Services.AddDbContext<DataBase>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddControllers();

            var app = builder.Build();

            // Initialize database on startup
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DataBase>();
                try
                {
                    // Принудительно создаем базу данных если ее нет
                    var created = db.Database.EnsureCreated();
                    Console.WriteLine($"Database ensured created on startup: {created}");

                    // Проверяем соединение
                    var canConnect = db.Database.CanConnect();
                    Console.WriteLine($"Database can connect: {canConnect}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Database initialization failed: {ex.Message}");
                }
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            //app.UseHttpsRedirection();

            app.UseHttpLogging();

            app.UseStaticFiles();

            app.UseRouting();

            app.MapControllers();

            app.MapBlazorHub();
            app.MapFallbackToPage("/_Host");

            app.MapGet("/api/health", async (HttpContext context) =>
            {
                var healthStatus = new
                {
                    status = "Healthy"
                };

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = 200;
                await context.Response.WriteAsync(JsonSerializer.Serialize(healthStatus));
            });

            app.MapGet("/api/backup", async (HttpContext context) =>
            {
                string? backupContent = DataBase.StaticBackup();

                context.Response.ContentType = "application/json";
                if (backupContent != null)
                {
                    context.Response.StatusCode = 200;
                    await context.Response.WriteAsync(JsonSerializer.Serialize(backupContent));
                }
                else
                {
                    context.Response.StatusCode = 503;
                }
            });

            app.Run();
        }
    }
}
