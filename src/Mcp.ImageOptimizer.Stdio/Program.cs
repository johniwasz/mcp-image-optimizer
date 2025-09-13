using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using ModelContextProtocol;
using Mcp.ImageOptimizer.Tools;


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

            builder.Logging.AddConsole(options =>
            {
                options.LogToStandardErrorThreshold = LogLevel.Trace;
            });

            await builder.Build().RunAsync();
        }
    }
}
