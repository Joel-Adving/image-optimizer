using System.Net;
using NetVips;

namespace ImageOptimizerV2.Services;

public class ImageProcessorService(
    HttpClient httpClient,
    IImageCacheService cacheService) : IImageProcessorService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly IImageCacheService _cacheService = cacheService;

    private readonly string[] allowedFormats = ["webp", "jpeg", "png" ];

    public async Task<byte[]> ProcessImageAsync(
        string imageUrl, int width, int height, string format, int quality
    )
    {
        ThrowIfArgumentsInvalid(imageUrl, width, height, format, quality);

        var cacheKey = _cacheService.GenerateCacheKey(
            imageUrl, width, height, format, quality
        );

        var cachedImage = await _cacheService.GetCachedImageAsync(cacheKey);
        if (cachedImage != null)
        {
            return cachedImage;
        }

        
        var imageBytes = await _httpClient.GetByteArrayAsync(imageUrl);
        using var image = Image.NewFromBuffer(imageBytes);
        
        if (width == 0 && height == 0)
        {
            var result = ConvertToFormat(image, format, quality);
            await _cacheService.CacheImageAsync(cacheKey, result);
            return result;
        }
        
        if (width > 0 && height == 0)
        {
            double ratio = (double)image.Width / image.Height;
            height = (int)(width / ratio);
        }
        
        if (height > 0 && width == 0)
        {
            double ratio = (double)image.Width / image.Height;
            width = (int)(height * ratio);
        }
        
        using var resizedImage = image.ThumbnailImage(width, height, crop: Enums.Interesting.Centre);
        var processedImage = ConvertToFormat(resizedImage, format, quality);
        await _cacheService.CacheImageAsync(cacheKey, processedImage);
        
        return processedImage;

    }

    private static byte[] ConvertToFormat(Image image, string format, int quality)
    {
        return format.ToLower() switch
        {
            "webp" => image.WebpsaveBuffer(quality),
            "jpeg" or "jpg" => image.JpegsaveBuffer(quality),
            "png" => image.PngsaveBuffer(9),
            _ => image.WebpsaveBuffer(quality),
        };
    }

    private void ThrowIfArgumentsInvalid(
        string? imageUrl, int? width, int? height, string? format, int? quality
    )
    {
        if (string.IsNullOrEmpty(imageUrl)) throw new ArgumentException("imageUrl is required.");
        if (string.IsNullOrEmpty(format) || !allowedFormats.Contains(format)) throw new ArgumentException("Invalid format.");
        if (quality == null || quality < 0 || quality > 100) throw new ArgumentException("Quality must be between 0 and 100.");
        if (width == null || height == null || width < 0 || height < 0) throw new ArgumentException("Width and height must be greater than or equal to 0.");
    }
}
