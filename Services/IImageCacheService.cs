namespace ImageOptimizerV2.Services;

public interface IImageCacheService
{
    Task<byte[]?> GetCachedImageAsync(string cacheKey);
    Task CacheImageAsync(string cacheKey, byte[] imageData);
    string GenerateCacheKey(
        string imageUrl, int width, int height, string format, int quality
    );
    void ClearCache();
}
