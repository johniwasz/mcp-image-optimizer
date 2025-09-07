using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mcp.ImageOptimizer.Common
{
    public class ConvertedImageMetadata : ImageMetadata
    {
        public ConvertedImageMetadata()
        {
        }

        public ConvertedImageMetadata(ImageMetadata imageMetadata)
        {
            if (imageMetadata != null)
            {
                this.Path = imageMetadata.Path;

                this.Height = imageMetadata.Height;
                this.Width = imageMetadata.Width;
                this.ResolutionFormat = imageMetadata.ResolutionFormat;
                this.VerticalResolution = imageMetadata.VerticalResolution;
                this.HorizontalResolution = imageMetadata.HorizontalResolution;
                this.Size = imageMetadata.Size;
                this.ExifData = imageMetadata.ExifData != null ? new Dictionary<string, string>(imageMetadata.ExifData) : new Dictionary<string, string>();
            }
        }

        [Description("Returns energy saved per Kilowatt hour for a single request of the imeage")]
        public double EnergySaved { get; set; }
    }
}
