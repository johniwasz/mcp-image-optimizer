using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mcp.ImageOptimizer.Tools
{
    public class ImageMetadata
    {
        public string? FilePath { get; set; }

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
            if (!string.IsNullOrEmpty(FilePath))
            {
                sb.AppendLine($"File Path: {FilePath}");
            }
            sb.AppendLine($"Width: {Width}");
            sb.AppendLine($"Height: {Height}");
            sb.AppendLine($"Size: {Size} bytes");
            if (!string.IsNullOrEmpty(ResolutionFormat))
            {
                sb.AppendLine($"Resolution Format: {ResolutionFormat}");
                sb.AppendLine($"Vertical Resolution: {VerticalResolution}");
                sb.AppendLine($"Horizontal Resolution: {HorizontalResolution}");
            }
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
