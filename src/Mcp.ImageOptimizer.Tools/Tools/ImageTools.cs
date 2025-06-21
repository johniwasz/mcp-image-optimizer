using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Globalization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Bmp;

namespace Mcp.ImageOptimizer.Tools.Tools
{
    [McpServerToolType]
    public sealed class ImageTools
    {
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
            return await GetImageMetadata(outputPath);
        }

        [McpServerTool, Description("Optimize an image in its current format by reducing file size while maintaining quality and return metadata for the optimized file.")]
        public static async Task<ImageMetadata?> OptimizeImage(
            [Description("The fully qualified path to an image file.")] string imageFilePath,
            [Description("Prefix to add to the optimized file name. Default is 'opt-'.")] string prefix = "opt-")
        {
            if (!File.Exists(imageFilePath))
            {
                throw new FileNotFoundException($"The specified file does not exist: {imageFilePath}");
            }

            if (string.IsNullOrWhiteSpace(prefix))
            {
                throw new ArgumentException("Prefix cannot be null or empty.", nameof(prefix));
            }

            // Generate output file path with prefix
            var directory = Path.GetDirectoryName(imageFilePath);
            var fileName = Path.GetFileName(imageFilePath);
            var outputPath = Path.Combine(directory ?? "", $"{prefix}{fileName}");

            // Load the image and optimize based on format
            using (var image = await Image.LoadAsync<Rgba32>(imageFilePath))
            {
                var format = await Image.DetectFormatAsync(imageFilePath);
                
                if (format == null)
                {
                    throw new NotSupportedException($"The image format is not supported: {imageFilePath}");
                }

                // Save with format-specific optimization
                if (format.Name.Equals("JPEG", StringComparison.OrdinalIgnoreCase))
                {
                    var encoder = new JpegEncoder()
                    {
                        Quality = 85 // High quality with good compression
                    };
                    await image.SaveAsync(outputPath, encoder);
                }
                else if (format.Name.Equals("PNG", StringComparison.OrdinalIgnoreCase))
                {
                    var encoder = new PngEncoder()
                    {
                        CompressionLevel = PngCompressionLevel.BestCompression
                    };
                    await image.SaveAsync(outputPath, encoder);
                }
                else if (format.Name.Equals("GIF", StringComparison.OrdinalIgnoreCase))
                {
                    var encoder = new GifEncoder();
                    await image.SaveAsync(outputPath, encoder);
                }
                else if (format.Name.Equals("BMP", StringComparison.OrdinalIgnoreCase))
                {
                    var encoder = new BmpEncoder();
                    await image.SaveAsync(outputPath, encoder);
                }
                else if (format.Name.Equals("WEBP", StringComparison.OrdinalIgnoreCase))
                {
                    var encoder = new WebpEncoder()
                    {
                        Quality = 85
                    };
                    await image.SaveAsync(outputPath, encoder);
                }
                else
                {
                    throw new NotSupportedException($"Optimization is not supported for image format: {format.Name}");
                }
            }

            // Get metadata for the optimized file
            return await GetImageMetadata(outputPath);
        }


    }
}