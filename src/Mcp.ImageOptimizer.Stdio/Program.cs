using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mcp.ImageOptimizer.Common;
using Mcp.ImageOptimizer.Stdio.Tools;
using Mcp.ImageOptimizer.Stdio.Prompts;
using Mcp.ImageOptimizer.Stdio.Resources;

namespace Mcp.ImageOptimizer.Stdio
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.Services
                .AddMcpServer()
                .WithStdioServerTransport()
                .WithPrompts<SimpleImagePrompts>()
                .WithPrompts<ImageComplexPrompts>()
                .WithResources<StaticResources>()
                .WithTools<ImageTools>();

            builder.Services.AddScoped<IImageConversionService, ImageConversionService>();

            builder.Logging.AddConsole(options =>
            {
#if DEBUG
                options.LogToStandardErrorThreshold = LogLevel.Trace;
#else
                options.LogToStandardErrorThreshold = LogLevel.Warning;
#endif
            });

            await builder.Build().RunAsync();
        }
    }
}
