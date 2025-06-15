using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using ModelContextProtocol;
using Mcp.ImageOptimizer.Tools.Tools;

namespace Mcp.ImageOptimizer.Tools
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

            builder.Services.AddSingleton(_ =>
            {
                var client = new HttpClient() { BaseAddress = new Uri("https://api.weather.gov") };
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("weather-tool", "1.0"));
                return client;
            });

            await builder.Build().RunAsync();
        }
    }
}
