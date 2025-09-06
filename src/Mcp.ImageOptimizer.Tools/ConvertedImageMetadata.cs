using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mcp.ImageOptimizer.Tools
{
    public class ConvertedImageMetadata : ImageMetadata
    {
        [Description("Returns energy saved per Kilowatt hour for a single request of the imeage")]
        public double EnergySaved { get; set; }
    }
}
