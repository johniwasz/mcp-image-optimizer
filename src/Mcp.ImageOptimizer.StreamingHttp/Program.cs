using Mcp.ImageOptimizer.Azure.Services;
using Mcp.ImageOptimizer.Common;
using Mcp.ImageOptimizer.StreamingHttp.Prompts;
using Mcp.ImageOptimizer.StreamingHttp.Tools;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.AspNetCore.Authentication;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Security.Claims;

const bool USE_OAUTH = false;

var builder = WebApplication.CreateBuilder(args);

var serverUrl = "http://localhost:5000/";
var appRegistrationOAuthServerUrl = "https://localhost:7029";


builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithPrompts<ImageBlobPrompts>()
    .WithPrompts<ImageBlobComplexPrompts>()
    .WithTools<AzureBlobTools>()
    .WithTools<AzureStorageTools>();

if (USE_OAUTH)
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        // Configure to validate tokens from our in-memory OAuth server
        options.Authority = appRegistrationOAuthServerUrl;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidAudience = serverUrl, // Validate that the audience matches the resource metadata as suggested in RFC 8707
            ValidIssuer = appRegistrationOAuthServerUrl,
            NameClaimType = "name",
            RoleClaimType = "roles"
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var name = context.Principal?.Identity?.Name ?? "unknown";
                var email = context.Principal?.FindFirstValue("preferred_username") ?? "unknown";
                Console.WriteLine($"Token validated for: {name} ({email})");
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                Console.WriteLine($"Challenging client to authenticate with Entra ID");
                return Task.CompletedTask;
            }
        };
    })
    .AddMcp(options =>
    {
        options.ResourceMetadata = new()
        {
            Resource = new Uri(serverUrl),
            AuthorizationServers = { new Uri(appRegistrationOAuthServerUrl) },
            ScopesSupported = ["mcp:tools"],
        };
    });

    builder.Services.AddAuthorization();
}


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

if (USE_OAUTH)
{
    app.UseAuthentication();
    app.UseAuthorization();

    // Use the default MCP policy name that we've configured
    app.MapMcp().RequireAuthorization();
}
else
{
    app.MapMcp();
}


app.Run(serverUrl);
