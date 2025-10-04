using Mcp.ImageOptimizer.Common.Models;
using SixLabors.ImageSharp;

namespace Mcp.ImageOptimizer.Common;

/// <summary>
/// Provides methods for converting images and retrieving image metadata.
/// </summary>
public interface IImageConversionService
{
    /// <summary>
    /// Converts the provided image stream to WebP format asynchronously.
    /// </summary>
    /// <param name="memStream">The memory stream containing the image data.</param>
    /// <param name="quality">The quality of the output WebP image (0-100).</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a <see cref="MemoryStream"/> with the converted WebP image.
    /// </returns>
    Task<MemoryStream> ConvertToWebPAsync(MemoryStream memStream, int quality, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts metadata from a loaded image.
    /// </summary>
    /// <param name="loadedImage">The loaded <see cref="Image"/> instance.</param>
    /// <param name="path">The file path or identifier of the image.</param>
    /// <param name="size">The size of the image in bytes.</param>
    /// <returns>
    /// An <see cref="ImageMetadata"/> object containing metadata about the image.
    /// </returns>
    ImageMetadata GetImageMetadata(Image loadedImage, string path, long size);

    /// <summary>
    /// Asynchronously retrieves image metadata from a file.
    /// </summary>
    /// <param name="imageFilePath">The file path of the image.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains an <see cref="ImageMetadata"/> object if successful; otherwise, <c>null</c>.
    /// </returns>
    Task<ImageMetadata?> GetImageMetadataFromFileAsync(string imageFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves image metadata from a memory stream.
    /// </summary>
    /// <param name="memoryStream">The memory stream containing the image data.</param>
    /// <param name="streamPath">The identifier or path associated with the stream.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains an <see cref="ImageMetadata"/> object with the image metadata.
    /// </returns>
    Task<ImageMetadata> GetImageMetadataFromStreamAsync(MemoryStream memoryStream, string streamPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether the specified MIME type represents a large image format.
    /// </summary>
    /// <param name="mimeType">The MIME type to check.</param>
    /// <returns>
    /// <c>true</c> if the MIME type is considered a large image format; otherwise, <c>false</c>.
    /// </returns>
    bool IsLargeImageMimeType(string mimeType);
}