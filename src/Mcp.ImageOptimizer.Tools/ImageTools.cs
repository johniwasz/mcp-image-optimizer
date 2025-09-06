using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Globalization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Webp;

namespace Mcp.ImageOptimizer.Tools
{
    [McpServerToolType]
    public sealed class ImageTools
    {
        private const long GIGABYTES = 1024 * 1024 * 1024; // 1 GB in bytes

        [McpServerTool, Description("Get image metadata including height, width, and EXIF data if it is available.")]
        public static async Task<ImageMetadata?> GetImageMetadata(
            [Description("The fully qualified path to an image file.")] string imageFilePath)
        {
            ImageMetadata? imageMetadata = null;

            if(File.Exists(imageFilePath))
            {
                imageMetadata = new ImageMetadata
                {
                    FilePath = imageFilePath
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
        public static async Task<ImageMetadata?> ConvertToWebP(
            [Description("The fully qualified path to an image file.")] string imageFilePath,
            [Description("Quality level for WebP compression (0-100, where 100 is lossless). Default is 90.")] int quality = 90)
        {
            if (!File.Exists(imageFilePath))
            {
                throw new FileNotFoundException($"The specified file does not exist: {imageFilePath}");
            }

            if (quality < 0 || quality > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(quality), "Quality must be between 0 and 100.");
            }

            long originalImageSize = new FileInfo(imageFilePath).Length;

            // Generate output file path - same directory and name, but with .webp extension
            var directory = Path.GetDirectoryName(imageFilePath);
            var filenameWithoutExtension = Path.GetFileNameWithoutExtension(imageFilePath);
            var outputPath = Path.Combine(directory ?? "", $"{filenameWithoutExtension}.webp");

            // Load the image and save as WebP
            using (var image = await Image.LoadAsync<Rgba32>(imageFilePath))
            {
                var encoder = new WebpEncoder()
                {
                    Quality = quality
                };

                await image.SaveAsync(outputPath, encoder);
            }

            // Get metadata for the new WebP file
            ImageMetadata imageData = await GetImageMetadata(outputPath);

            ConvertedImageMetadata convertedMetadata = new ConvertedImageMetadata
            {
                FilePath = outputPath,
                Width = imageData.Width,
                Height = imageData.Height,
                Size = imageData.Size,
                ResolutionFormat = imageData.ResolutionFormat,
                VerticalResolution = imageData.VerticalResolution,
                HorizontalResolution = imageData.HorizontalResolution,
                ExifData = imageData.ExifData
            };

            long bytesSaved = originalImageSize - convertedMetadata.Size;

            convertedMetadata.EnergySaved = bytesSaved/GIGABYTES * 0.81;

            return convertedMetadata;
        }


    }
}