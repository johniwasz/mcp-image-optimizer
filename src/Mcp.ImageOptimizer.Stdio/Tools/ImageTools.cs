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

[McpServerToolType]
internal class ImageTools
{

    [McpServerTool, Description("Get image metadata including height, width, and EXIF data if it is available.")]
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

    [McpServerTool, Description("Convert an image to WebP format with configurable quality and return metadata for the new file.")]
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

        // Change this line:
        // ImageMetadata imageData = await GetImageMetadata(outputPath);
        // To the following, to handle possible null return value:
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