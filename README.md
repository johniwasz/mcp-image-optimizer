# Introduction to mcp-image-optimizer

This repo includes two Model Context Protocol servers inspired by [Green Software Patterns](https://patterns.greensoftware.foundation/). Rather than another simple weather api MCP server, I wanted to generate two examples that would resonate with a realistic use case.

[Green Software Patterns](https://patterns.greensoftware.foundation/) provide guidance to reduce the carbon footprint broken down into three categories:

- [Artificial Intelligence (AI)](https://patterns.greensoftware.foundation/catalog/ai/)
- [Cloud](https://patterns.greensoftware.foundation/catalog/cloud/)
- [Web](https://patterns.greensoftware.foundation/catalog/web/)

Some rules are easier to programmatically enforce than others. [Encrypting what is necessary](https://patterns.greensoftware.foundation/catalog/cloud/encrypt-what-is-necessary) requires business knowledge that's suitable for an agent grounded on architecture documentation and business rules, but doesn't lend itself to a Model Context Protocol Server. 

MCP servers extend the native capabilities of the LLM with a REST-like service. The two examples in this repo are inspired by the [Serve images in modern formats](https://patterns.greensoftware.foundation/catalog/web/serve-images-in-modern-formats) Green Software pattern. 

The goal of the two MCP servers is to search for PNG, JPG, and GIF and convert them to [WebP](https://developers.google.com/speed/webp) using [SixLabors.ImageSharp](https://www.nuget.org/packages/SixLabors.ImageSharp). 

## MCP Servers

### Mcp.ImageOptimizer.Stdio

Designed to run locally in Visual Studio Code using [stdio](https://modelcontextprotocol.io/specification/2025-06-18/basic/transports#stdio). This traverses the loaded workspace, identifies image files, and converts image files. The MCP server runs as a subprocess and uses the same identity as the VS Code user. Images are identified using the file extension (PNG, JPG, GIF) and converted to WebP.

| Project Name | Type | Description | Purpose |
| --- | --- | --- | --- |
| Mcp.ImageOptimizer.Stdio | Executable (Console) |	MCP server using stdio transport for local image optimization	|	Provides MCP tools and resources for image metadata extraction and WebP conversion via standard input/output. Includes prompts and resources for local file system operations. |

#### stdio Configuration for local testing

This MCP configuration compiles and launches the Mcp.ImageOptimizer.Stdio server. It's useful for debugging.

``` json
{
  "servers": {
    "imageoptimizer": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "-configuration",
        "Debug",
        "--no-build",
        "--project",
        "C:\\Users\\iwasz\\source\\repos\\mcp-image-optimizer\\src\\Mcp.ImageOptimizer.Stdio\\Mcp.ImageOptimizer.Stdio.csproj"
      ]
    }
  }
}
```
### Mcp.ImageOptimizer.StreamingHttp

Uses Kestrel to host a [Streamable HTTP](https://modelcontextprotocol.io/specification/2025-06-18/basic/transports#streamable-http) MCP server. Uses cached Azure credentials to identify Azure Blob containers. Blob images are identified using the MIME type and converted to WebP. Authentication is managed using an Entra app registration. This is not a recommended approach for a production application. Ideally, OAuth or Entra should be used for authenication (see [MCP Entra Examples](https://github.com/Azure-Samples/mcp-auth-servers?tab=readme-ov-file)). This app registration uses a trusted-subsystem model and leads to an overprivileged service.

| Project Name | Type | Description |
| --- | --- | --- |
| Mcp.ImageOptimizer.StreamingHttp | Executable (Web/Console) |	MCP server using HTTP transport for Azure Blob operations and to perform image conversion. |
| Mcp.ImageOptimizer.Azure.Services | Class Library	|	Azure-specific services for blob and resource management. Implements IBlobService and IAzureResourceService for interacting with Azure Storage Accounts, managing blob images, and handling Azure authentication (DefaultAzureCredential, ClientSecretCredential). |

#### Streamable HTTP configuration
``` json
{
  "servers": {
    "imageoptimizer-azure": {
      "type": "http",
      "monitor": true,

      "url": "http://localhost:5000"
    }
  }
}
```
## Common Projects

| Project Name | Type | Purpose |
| --- | --- | --- | 
| Mcp.ImageOptimizer.Common |	Class Library | Provides core image conversion services, models (ImageMetadata, ConvertedImageMetadata), and common functionality used across MCP servers. Includes energy savings calculations. |
| Mcp.ImageOptimizer.Tools.Tests | Test Project	| Uses xUnit framework with code coverage analysis. Not comprehensive. Needs work |

## Resources

| Link | Description |
|---|---|
| [Model Context Protocol](https://modelcontextprotocol.org) | Starting Point for the MCP Specification and Backgroi |
| [Model Context Protocol Git repo](https://github.com/modelcontextprotocol) | GitHub launching point for all things MCP |
| [MCP-Checklist](https://github.com/MCP-Manager/MCP-Checklists) | Helpful guide for securing MCP Servers |
| [MCP Entra Examples](https://github.com/Azure-Samples/mcp-auth-servers?tab=readme-ov-file) | Examples of MCP Servers using Entra for authentication |
| [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk) | C# SDK for building MCP Servers and Clients |
| [MCP Inspector](https://github.com/modelcontextprotocol/inspector) | Tool for inspecting MCP messages and debugging |
| [Building Your First MCP Server with .NET and Publishing to NuGet](https://devblogs.microsoft.com/dotnet/mcp-server-dotnet-nuget-quickstart/) | Publish MCP servers on Nuget |
| [Awesome-MCP-Servers](https://github.com/punkpeye/awesome-mcp-servers) | Curated list of MCP Servers |
| [Awesome-MCP-Clients](https://github.com/punkpeye/awesome-mcp-clients) | Curated list of MCP Clients |
| [MCP Shield](https://github.com/Jitha-afk/MCPShield) | Firewall for MCP Servers generated from a Microsoft Hackathon |
| [MCP for Beginners](https://github.com/microsoft/mcp-for-beginners) | Getting started guides in multiple languages and principles of MCP |
