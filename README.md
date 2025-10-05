# mcp-image-optimizer
Optimizes image sizes for screen displays in accordance with Green Software Foundation principles.



## Sample MPC Server Configurations


### STDIO Configuration for local testing

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

