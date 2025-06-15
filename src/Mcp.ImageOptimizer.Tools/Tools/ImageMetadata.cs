using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mcp.ImageOptimizer.Tools.Tools
{
    public class ImageMetadata
    {

        public int Width { get; set; }

        public int Height { get; set; }

        public int Size { get; set; }

        public string? ResolutionFormat { get; set; }

        public double VerticalResolution { get; set; }

        public double HorizontalResolution { get; set; }  

        public Dictionary<string, string> ExifData { get; set; }

        public ImageMetadata()
        {
            ExifData = new Dictionary<string, string>();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Width: {Width}");
            sb.AppendLine($"Height: {Height}");
            if (ExifData.Any())
            {
                sb.AppendLine("EXIF Data:");
                foreach (var kvp in ExifData)
                {
                    sb.AppendLine($"{kvp.Key}: {kvp.Value}");
                }
            }
            return sb.ToString();
        }
    }
}
