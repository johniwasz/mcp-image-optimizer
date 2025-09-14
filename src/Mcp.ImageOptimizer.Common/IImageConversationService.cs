using SixLabors.ImageSharp;

namespace Mcp.ImageOptimizer.Common
{
    public interface IImageConversationService
    {
        Task<MemoryStream> ConvertToWebPAsync(MemoryStream memStream, int quality);
        ImageMetadata GetImageMetadata(Image loadedImage, string path, long size);
        Task<ImageMetadata?> GetImageMetadataFromFileAsync(string imageFilePath);
        Task<ImageMetadata> GetImageMetadataFromStreamAsync(MemoryStream memoryStream, string streamPath);
    }
}