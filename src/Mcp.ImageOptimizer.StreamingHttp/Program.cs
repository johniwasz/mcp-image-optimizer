using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using ModelContextProtocol;
using ModelContextProtocol.AspNetCore;
using Mcp.ImageOptimizer.StreamingHttp.Tools;
using Microsoft.Extensions.Azure;
using Mcp.ImageOptimizer.Azure.Tools;
using Mcp.ImageOptimizer.Common;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<AzureBlobTools>();

builder.Services.AddOpenTelemetry()
    .WithTracing(b => b.AddSource("*")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation())
    .WithMetrics(b => b.AddMeter("*")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation())
    .WithLogging()
    .UseOtlpExporter();

builder.Services
    .AddScoped<IBlobService, BlobService>()
    .AddScoped<IImageConversionService, ImageConversionService>()
    .AddScoped<IAzureResourceService, AzureResourceService>();

var app = builder.Build();

app.MapMcp();

app.Run();
