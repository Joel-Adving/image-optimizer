namespace ImageOptimizerV2.Services;

public class CacheCleanupService(IConfiguration configuration, ILogger<CacheCleanupService> logger) : BackgroundService
{
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<CacheCleanupService> _logger = logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(double.Parse(configuration["ImageCache:CleanupIntervalHours"] ?? "6"));
    private readonly TimeSpan _cacheTtl = TimeSpan.FromHours(double.Parse(configuration["ImageCache:TtlHours"] ?? "24"));
    private readonly string _cacheDirectory = configuration["ImageCache:Directory"] ?? Path.Combine(Path.GetTempPath(), "ImageOptimizerCache");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Cache cleanup service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupCacheAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache cleanup");
            }

            await Task.Delay(_cleanupInterval, stoppingToken);
        }
    }

    private async Task CleanupCacheAsync()
    {
        _logger.LogInformation("Starting cache cleanup");
        
        if (!Directory.Exists(_cacheDirectory))
        {
            return;
        }

        int deletedCount = 0;
        long freedBytes = 0;

        await Task.Run(() =>
        {
            var expiredFiles = Directory
                .EnumerateFiles(_cacheDirectory, "*", SearchOption.AllDirectories)
                .Select(f => new FileInfo(f))
                .Where(f => DateTime.UtcNow - f.LastAccessTime > _cacheTtl)
                .ToList();

            foreach (var file in expiredFiles)
            {
                try
                {
                    long size = file.Length;
                    file.Delete();
                    deletedCount++;
                    freedBytes += size;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete expired file {FilePath}", file.FullName);
                }
            }

            CleanEmptyDirectories(_cacheDirectory);
        });

        _logger.LogInformation("Cache cleanup completed. Deleted {DeletedCount} files, freed {FreedMB:F2} MB", 
            deletedCount, freedBytes / (1024.0 * 1024.0));
    }

    private void CleanEmptyDirectories(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        foreach (var dir in Directory.GetDirectories(directory))
        {
            CleanEmptyDirectories(dir);
        }

        if (!Directory.EnumerateFileSystemEntries(directory).Any() && directory != _cacheDirectory)
        {
            try
            {
                Directory.Delete(directory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete empty directory {Directory}", directory);
            }
        }
    }
}
