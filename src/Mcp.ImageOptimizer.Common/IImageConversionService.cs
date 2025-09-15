using Mcp.ImageOptimizer.Common.Models;
using SixLabors.ImageSharp;

namespace Mcp.ImageOptimizer.Common;

public interface IImageConversionService
{
    Task<MemoryStream> ConvertToWebPAsync(MemoryStream memStream, int quality, CancellationToken cancellationToken = default);
    ImageMetadata GetImageMetadata(Image loadedImage, string path, long size);
    Task<ImageMetadata?> GetImageMetadataFromFileAsync(string imageFilePath, CancellationToken cancellationToken = default);
    Task<ImageMetadata> GetImageMetadataFromStreamAsync(MemoryStream memoryStream, string streamPath, CancellationToken cancellationToken = default);
    bool IsLargeImageMimeType(string mimeType);
}