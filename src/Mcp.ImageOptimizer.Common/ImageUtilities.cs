using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mcp.ImageOptimizer.Common
{
    public static class ImageUtilities
    {

        public static async Task<ImageMetadata?> GetImageMetadataFromFileAsync(string imageFilePath)
        {
            ImageMetadata? imageMetadata = null;

            if (File.Exists(imageFilePath))
            {
                using var image = await Image.LoadAsync<Rgba32>(imageFilePath);

                imageMetadata = GetImageMetadata(image, imageFilePath, (int)new FileInfo(imageFilePath).Length);
            }
            else
            {
                throw new FileNotFoundException($"The specified file does not exist: {imageFilePath}");
            }

            return imageMetadata;
        }

        public static async Task<ImageMetadata> GetImageMetadataFromStreamAsync(MemoryStream memoryStream, string streamPath)
        {
            ImageMetadata? imageMetadata = null;

            using var image = await Image.LoadAsync<Rgba32>(memoryStream);

            imageMetadata = GetImageMetadata(image, streamPath, memoryStream.Length);



            return imageMetadata;
        }

        public static ImageMetadata GetImageMetadata(Image loadedImage, string path, long size)
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

        public static async Task<MemoryStream> ConvertToWebPAsync(MemoryStream memStream, int quality)
        {
            // Load the image and save as WebP

            memStream.Position = 0;

            MemoryStream webPStream = new();

            using (var image = await Image.LoadAsync<Rgba32>(memStream))
            {
                var encoder = new WebpEncoder()
                {
                    Quality = quality
                };

                await image.SaveAsync(webPStream, encoder);
            }

            webPStream.Position = 0;

            return webPStream;
        }
    }
}
