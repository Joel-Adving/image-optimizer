using ImageOptimizerV2.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

NetVips.NetVips.Init();

builder.Services.AddOpenApi();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IImageProcessorService, ImageProcessorService>();
builder.Services.AddSingleton<IImageCacheService, FileSystemImageCacheService>();
builder.Services.AddHostedService<CacheCleanupService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("WhitelistPolicy", policy =>
    {
        policy.WithOrigins("https://case.oki.gg")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("WhitelistPolicy");

app.MapGet("", async (
    ILogger<Program> _logger,
    IImageProcessorService _imageProcessorService,
    [FromQuery(Name = "url")] string imageUrl,
    [FromQuery(Name = "w")] decimal? _width,
    [FromQuery(Name = "h")] decimal? _height,
    [FromQuery(Name = "f")] string? _format, 
    [FromQuery(Name = "q")] decimal? _quality) =>
{
    var quality = _quality ?? 80;
    var format = _format ?? "webp";
    var width = _width ?? 0;
    var height = _height ?? 0;

    try
    {
        var optimizedImageBytes = await _imageProcessorService.ProcessImageAsync(
            imageUrl, (int)width, (int)height, format, (int)quality
        );
        return Results.File(optimizedImageBytes, $"image/{format}");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to process image: {Url}", imageUrl);
        return Results.StatusCode(500);
    }
});

app.MapGet("clear-cache", (
    ILogger<Program> _logger,
    IImageCacheService _cacheService) =>
{
    _logger.LogInformation("Clearing cache");
    _cacheService.ClearCache();
    return Results.Ok();
});

app.Run();