using System.ComponentModel;

namespace Mcp.ImageOptimizer.Common.Models;

public class ConvertedImageMetadata : ImageMetadata
{
    public ConvertedImageMetadata()
    {
    }

    public ConvertedImageMetadata(ImageMetadata imageMetadata)
    {
        if (imageMetadata != null)
        {
            Path = imageMetadata.Path;

            Height = imageMetadata.Height;
            Width = imageMetadata.Width;
            ResolutionFormat = imageMetadata.ResolutionFormat;
            VerticalResolution = imageMetadata.VerticalResolution;
            HorizontalResolution = imageMetadata.HorizontalResolution;
            Size = imageMetadata.Size;
            ExifData = imageMetadata.ExifData != null ? new Dictionary<string, string>(imageMetadata.ExifData) : new Dictionary<string, string>();
        }
    }

    [Description("Returns energy saved per Kilowatt hour for a single request of the imeage")]
    public double EnergySaved { get; set; }
}
