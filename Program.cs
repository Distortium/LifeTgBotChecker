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
                options.UseSqlite("Data Source=LifeCheckDataBase.db"));

            builder.Services.AddControllers();

            var app = builder.Build();

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
