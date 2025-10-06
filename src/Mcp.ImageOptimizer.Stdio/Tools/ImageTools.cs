using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Globalization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Webp;
using Mcp.ImageOptimizer.Common.Models;
using Mcp.ImageOptimizer.Common;

namespace Mcp.ImageOptimizer.Stdio.Tools;

/// <summary>
/// Provides image-related tools for metadata extraction and format conversion.
/// </summary>
[McpServerToolType]
internal class ImageTools
{
    /// <summary>
    /// Gets image metadata including height, width, and EXIF data if available.
    /// </summary>
    /// <param name="imageConversionService">The image conversion service to use for metadata extraction.</param>
    /// <param name="imageFilePath">The fully qualified path to an image file.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// An <see cref="ImageMetadata"/> object containing metadata about the image, or <c>null</c> if the file does not exist.
    /// </returns>
    /// <exception cref="FileNotFoundException">Thrown if the specified file does not exist.</exception>
    [McpServerTool(ReadOnly = false, Destructive = false, Idempotent = true, Name = "get_image_metadata"), 
        Description("Get image metadata including height, width, and EXIF data if it is available.")]
    internal async Task<ImageMetadata?> GetImageMetadataAsync(
        IImageConversionService imageConversionService,
        [Description("The fully qualified path to an image file.")] string imageFilePath,
        CancellationToken cancellationToken = default)
    {
        ImageMetadata? imageMetadata = null;

        if(File.Exists(imageFilePath))
        {
            imageMetadata = new ImageMetadata
            {
                Path = imageFilePath
            };

            using (var image = await Image.LoadAsync<Rgba32>(imageFilePath))
            {
                imageMetadata.Width = image.Width;
                imageMetadata.Height = image.Height;
                imageMetadata.Size = (int)new FileInfo(imageFilePath).Length;

                if (image.Metadata != null)
                {
                    imageMetadata.ResolutionFormat = image.Metadata.ResolutionUnits.ToString();
                    imageMetadata.VerticalResolution = image.Metadata.VerticalResolution;
                    imageMetadata.HorizontalResolution = image.Metadata.HorizontalResolution;

                    if (image.Metadata?.ExifProfile?.Values != null)
                    {
                        foreach (var prop in image.Metadata.ExifProfile.Values)
                        {
                            imageMetadata.ExifData[prop.Tag.ToString()] = prop?.GetValue()?.ToString() ?? string.Empty;
                        }
                    }
                }           
            }
        }
        else
        {
            throw new FileNotFoundException($"The specified file does not exist: {imageFilePath}");
        }

        return imageMetadata;
    }

    /// <summary>
    /// Converts an image to WebP format with configurable quality and returns metadata for the new file.
    /// </summary>
    /// <param name="imageService">The image conversion service to use for conversion and metadata extraction.</param>
    /// <param name="imageFilePath">The fully qualified path to an image file.</param>
    /// <param name="quality">Quality level for WebP compression (0-100, where 100 is lossless). Default is 90.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="ConvertedImageMetadata"/> object containing metadata about the converted WebP image.
    /// </returns>
    /// <exception cref="McpException">Thrown if the file does not exist or the quality parameter is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown if metadata retrieval for the converted file fails.</exception>
    [McpServerTool(ReadOnly = false, Destructive = false, Idempotent = false, Name = "convert_image_to_webp"),
        Description("Convert an image to WebP format with configurable quality and return metadata for the new file.")]
    internal async Task<ImageMetadata?> ConvertToWebPAsync(
        IImageConversionService imageService,
        [Description("The fully qualified path to an image file.")] string imageFilePath,
        [Description("Quality level for WebP compression (0-100, where 100 is lossless). Default is 90.")] int quality = 90,  
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(imageFilePath))
        {
            throw new McpException($"The specified file does not exist: {imageFilePath}");
        }

        if (quality < 0 || quality > 100)
        {
            throw new McpException($"Quality {quality} must be between 0 and 100.", McpErrorCode.InvalidParams);
        }

        long originalImageSize = new FileInfo(imageFilePath).Length;

        // Generate output file path - same directory and name, but with .webp extension
        var directory = Path.GetDirectoryName(imageFilePath);
        var filenameWithoutExtension = Path.GetFileNameWithoutExtension(imageFilePath);
        var outputPath = Path.Combine(directory ?? "", $"{filenameWithoutExtension}.webp");

        // Load the image and save as WebP
        using (var image = await Image.LoadAsync<Rgba32>(imageFilePath, cancellationToken))
        {
            var encoder = new WebpEncoder()
            {
                Quality = quality
            };

            await image.SaveAsync(outputPath, encoder, cancellationToken);
        }

        ImageMetadata? imageData = await GetImageMetadataAsync(imageService, outputPath, cancellationToken);
        if (imageData == null)
        {
            throw new InvalidOperationException($"Failed to retrieve metadata for the converted WebP file: {outputPath}");
        }

        ConvertedImageMetadata convertedMetadata = new(imageData);

        long bytesSaved = originalImageSize - convertedMetadata.Size;

        convertedMetadata.EnergySaved = (bytesSaved/ImageMetadata.GIGABYTES) * 0.81;

        return convertedMetadata;
    }
}