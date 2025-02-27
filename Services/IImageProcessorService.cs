namespace ImageOptimizerV2.Services;

public interface IImageProcessorService
{
    Task<byte[]> ProcessImageAsync(
        string imageUrl, int width, int height, string format, int quality
    );
}
