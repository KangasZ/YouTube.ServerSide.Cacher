using System.Text.Json.Serialization;
using YouTube.ServerSide.Cacher.Configuration;
using YouTube.ServerSide.Cacher.Models;
using YouTube.ServerSide.Cacher.Services.SiteDownloader;
using YouTube.ServerSide.Cacher.Services.VideoManager;
using YouTube.ServerSide.Cacher.Services.YTDownloader;
using YouTube.ServerSide.Cacher.YTDownloader;

namespace YouTube.ServerSide.Cacher;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add configuration
        var appsettings = new AppSettings();
        builder.Configuration.Bind(appsettings);
        builder.Services.AddSingleton<AppSettings>(appsettings);

        // Add services to the container.
        builder.Services.AddAuthorization();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        builder.Services.AddSingleton<DownloadManager>();
        builder.Services.AddSingleton<PathManager>();
        builder.Services.AddSingleton<CacheManager>();
        builder.Services.AddHostedService<CacheCleanupService>();
        builder.Services.AddSingleton<YouTubeDownloader>();

        builder
            .Services.AddControllers()
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter())
            );

        var app = builder.Build();
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
            app.MapOpenApi();

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
