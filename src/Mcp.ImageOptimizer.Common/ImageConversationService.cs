using Mcp.ImageOptimizer.Common.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mcp.ImageOptimizer.Common;

public class ImageConversationService : IImageConversationService
{
    private HashSet<string> _largeImageTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/gif",
            "image/bmp",
            "image/tiff"
        };

    public async Task<ImageMetadata?> GetImageMetadataFromFileAsync(string imageFilePath, CancellationToken cancellationToken = default)
    {
        ImageMetadata? imageMetadata = null;

        if (File.Exists(imageFilePath))
        {
            using var image = await Image.LoadAsync<Rgba32>(imageFilePath, cancellationToken);

            imageMetadata = GetImageMetadata(image, imageFilePath, (int)new FileInfo(imageFilePath).Length);
        }
        else
        {
            throw new FileNotFoundException($"The specified file does not exist: {imageFilePath}");
        }

        return imageMetadata;
    }

    public async Task<ImageMetadata> GetImageMetadataFromStreamAsync(MemoryStream memoryStream, string streamPath, CancellationToken cancellationToken = default)
    {
        ImageMetadata? imageMetadata = null;

        using var image = await Image.LoadAsync<Rgba32>(memoryStream, cancellationToken);

        imageMetadata = GetImageMetadata(image, streamPath, memoryStream.Length);

        return imageMetadata;
    }

    public ImageMetadata GetImageMetadata(Image loadedImage, string path, long size)
    {
        if (loadedImage == null)
        {
            throw new ArgumentNullException(nameof(loadedImage));
        }

        var imageMetadata = new ImageMetadata
        {
            Path = path,
            Size = size,
            Width = loadedImage.Width,
            Height = loadedImage.Height
        };

        if (loadedImage.Metadata != null)
        {
            imageMetadata.ResolutionFormat = loadedImage.Metadata.ResolutionUnits.ToString();
            imageMetadata.VerticalResolution = loadedImage.Metadata.VerticalResolution;
            imageMetadata.HorizontalResolution = loadedImage.Metadata.HorizontalResolution;

            if (loadedImage.Metadata.ExifProfile?.Values != null)
            {
                foreach (var prop in loadedImage.Metadata.ExifProfile.Values)
                {
                    imageMetadata.ExifData ??= new Dictionary<string, string>();
                    imageMetadata.ExifData[prop.Tag.ToString()] = prop?.GetValue()?.ToString() ?? string.Empty;
                }
            }
        }

        return imageMetadata;
    }

    public async Task<MemoryStream> ConvertToWebPAsync(MemoryStream memStream, int quality, CancellationToken cancellationToken = default)
    {
        // Load the image and save as WebP

        memStream.Position = 0;

        MemoryStream webPStream = new();

        using (var image = await Image.LoadAsync<Rgba32>(memStream, cancellationToken))
        {
            var encoder = new WebpEncoder()
            {
                Quality = quality
            };

            await image.SaveAsync(webPStream, encoder, cancellationToken);
        }

        webPStream.Position = 0;

        return webPStream;
    }

    public bool IsLargeImageMimeType(string mimeType)
    {
        if (string.IsNullOrEmpty(mimeType))
        {
            return false;
        }

        return _largeImageTypes.Contains(mimeType);
    }
}
