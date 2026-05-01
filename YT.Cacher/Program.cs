using YT.Cacher.YTDownloader;

namespace YT.Cacher;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddAuthorization();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        builder.Services.AddSingleton<Downloader>();
        builder.Services.AddSingleton<PathManager>();
        builder.Services.AddSingleton<CacheManager>();
        builder.Services.AddHostedService<CacheCleanupService>();

        builder.Services.AddControllers();

        var app = builder.Build();
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment()) app.MapOpenApi();

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
