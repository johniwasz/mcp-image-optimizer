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