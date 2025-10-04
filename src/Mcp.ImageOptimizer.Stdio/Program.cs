using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using ModelContextProtocol;
using Mcp.ImageOptimizer.Common;
using Mcp.ImageOptimizer.Stdio.Tools;

namespace Mcp.ImageOptimizer.Stdio
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.Services.AddMcpServer()
                .WithStdioServerTransport()
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
