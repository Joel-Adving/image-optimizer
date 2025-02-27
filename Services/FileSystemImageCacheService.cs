using System.Security.Cryptography;
using System.Text;

namespace ImageOptimizerV2.Services;

public class FileSystemImageCacheService : IImageCacheService
{
    private readonly string _cacheDirectory;
    private readonly TimeSpan _cacheTtl;
    private readonly ILogger<FileSystemImageCacheService> _logger;

    public FileSystemImageCacheService(IConfiguration configuration, ILogger<FileSystemImageCacheService> logger)
    {
        _cacheDirectory = configuration["ImageCache:Directory"] ?? Path.Combine(Path.GetTempPath(), "ImageOptimizerCache");
        _cacheTtl = TimeSpan.FromHours(double.Parse(configuration["ImageCache:TtlHours"] ?? "24"));
        _logger = logger;

        if (!Directory.Exists(_cacheDirectory))
        {
            Directory.CreateDirectory(_cacheDirectory);
        }
    }

    public async Task<byte[]?> GetCachedImageAsync(string cacheKey)
    {
        var filePath = GetFilePathFromCacheKey(cacheKey);

        if (!File.Exists(filePath))
        {
            return null;
        }

        var fileInfo = new FileInfo(filePath);
        if (IsFileExpired(fileInfo))
        {
            _logger.LogInformation("Cache expired for {CacheKey}", cacheKey);
            try
            {
                File.Delete(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting expired cache file {FilePath}", filePath);
            }
            return null;
        }

        fileInfo.LastAccessTime = DateTime.UtcNow;
        return await File.ReadAllBytesAsync(filePath);
    }

    public async Task CacheImageAsync(string cacheKey, byte[] imageData)
    {
        var filePath = GetFilePathFromCacheKey(cacheKey);

        string directory = Path.GetDirectoryName(filePath)!;
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllBytesAsync(filePath, imageData);
    }

    public string GenerateCacheKey(
        string imageUrl, int width, int height, string format, int quality
    )
    {
        var key = $"{imageUrl}|w{width}|h{height}|f{format}|q{quality}";
        
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    public void ClearCache()
    {
        Directory.Delete(_cacheDirectory, true);
        Directory.CreateDirectory(_cacheDirectory);
        _logger.LogInformation("Cache cleared");
    }

    private string GetFilePathFromCacheKey(string cacheKey)
    {
        var subDir1 = cacheKey[..2];
        var subDir2 = cacheKey.Substring(2, 2);
        return Path.Combine(_cacheDirectory, subDir1, subDir2, cacheKey);
    }

    private bool IsFileExpired(FileInfo fileInfo)
    {
        return DateTime.UtcNow - fileInfo.LastAccessTime > _cacheTtl;
    }
}
