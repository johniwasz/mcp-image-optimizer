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



[Model Context Protocol Git repo](https://github.com/modelcontextprotocol)

[Model Context Protocol Specification](https://modelcontextprotocol.org)]

Helpful guide for securing MCP Servers [MCP-Checklist](https://github.com/MCP-Manager/MCP-Checklists)

[MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk)

[Awesome-MCP-Servers](https://github.com/punkpeye/awesome-mcp-servers)

[Awesome-MCP-Clients](https://github.com/punkpeye/awesome-mcp-clients)

[MCP Inspector](https://github.com/modelcontextprotocol/inspector)