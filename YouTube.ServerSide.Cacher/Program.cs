using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using YouTube.ServerSide.Cacher.Configuration;
using YouTube.ServerSide.Cacher.Models;
using YouTube.ServerSide.Cacher.Services;
using YouTube.ServerSide.Cacher.Services.CacheServices;
using YouTube.ServerSide.Cacher.Services.DownloadServices;
using YouTube.ServerSide.Cacher.Services.DownloadServices.SiteDownloader;
using YouTube.ServerSide.Cacher.Services.Protection;

namespace YouTube.ServerSide.Cacher;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add configuration
        builder.Services.Configure<AppSettings>(builder.Configuration);
        builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<AppSettings>>().Value);

        // Add services to the container.
        builder.Services.AddAuthorization();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        builder.Services.AddSingleton<DownloadManager>();
        builder.Services.AddSingleton<PathManager>();
        builder.Services.AddSingleton<CacheManager>();
        builder.Services.AddHostedService<CacheCleanupService>();
        builder.Services.AddSingleton<IYouTubeDownloader, YouTubeDownloader>();
        builder.Services.AddSingleton<IProtectionService, ProtectionService>();

        builder.Services.AddControllers();

        var app = builder.Build();
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.Use(
            async (ctx, next) =>
            {
                var h = ctx.Response.Headers;
                h["X-Content-Type-Options"] = "nosniff";
                h["X-Frame-Options"] = "DENY";
                h["Referrer-Policy"] = "no-referrer";
                h["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
                h["X-XSS-Protection"] = "0";
                await next();
            }
        );

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
