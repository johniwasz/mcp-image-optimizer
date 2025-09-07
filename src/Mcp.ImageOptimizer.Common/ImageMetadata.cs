using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mcp.ImageOptimizer.Common
{
    public class ImageMetadata
    {
        public const long GIGABYTES = 1024 * 1024 * 1024; // 1 GB in bytes

        public string? Path { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public long Size { get; set; }

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
            if (!string.IsNullOrEmpty(Path))
            {
                sb.AppendLine($"Path: {Path}");
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
