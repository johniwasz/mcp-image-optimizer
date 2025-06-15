using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Globalization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;

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
                imageMetadata = new ImageMetadata();
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


    }
}