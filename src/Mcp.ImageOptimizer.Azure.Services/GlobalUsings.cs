global using System;
global using System.IO;
global using System.Threading;
global using System.Threading.Tasks;
global using System.Collections.Generic;
global using System.Linq;

global using Azure;
global using Azure.Core;
global using Azure.Identity;
global using Azure.ResourceManager;
global using Azure.ResourceManager.Resources;
global using Azure.ResourceManager.Storage;
global using Azure.Storage.Blobs;
global using Azure.Storage.Blobs.Models;

global using SixLabors.ImageSharp;
global using SixLabors.ImageSharp.PixelFormats;
global using SixLabors.ImageSharp.Formats.Webp;

global using Mcp.ImageOptimizer.Common;
global using Mcp.ImageOptimizer.Common.Models;