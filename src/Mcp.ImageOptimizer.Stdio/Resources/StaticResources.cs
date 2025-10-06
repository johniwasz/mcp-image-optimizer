
﻿using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Mcp.ImageOptimizer.Stdio.Resources;

[McpServerResourceType]
public class StaticResources
{
    [McpServerResource(UriTemplate = "test://direct/text/resource", Name = "Readme Text Resource", MimeType = "text/markdown")]
    [Description("A direct text resource")]
    public static string ReadMeTextResource() => @"# MCP Tools for `Mcp.ImageOptimizer.Stdio`

This document provides an overview and usage guide for clients who want to consume the MCP Tools and resources provided by the `Mcp.ImageOptimizer.Stdio` package.

## Overview

`Mcp.ImageOptimizer.Stdio` exposes a set of server tools and resources for image processing and metadata extraction, designed to be used via the Model Context Protocol (MCP). These tools allow clients to:

- Retrieve image metadata (dimensions, EXIF, resolution, etc.)
- Convert images to WebP format with configurable quality
- Access sample resources for testing and demonstration

## Tools

### 1. `get_image_metadata`

**Description:**  
Get image metadata including height, width, and EXIF data if available.

**Parameters:**
- `imageFilePath` (string): Fully qualified path to the image file.

**Returns:**  
`ImageMetadata` object with properties such as `Width`, `Height`, `Size`, `ResolutionFormat`, `VerticalResolution`, `HorizontalResolution`, and `ExifData`.

**Example Usage:**
``` json
{ ""tool"": ""get_image_metadata"", ""params"": { ""imageFilePath"": ""C:\images\sample.jpg"" } }
```
---

### 2. `convert_image_to_webp`

**Description:**  
Convert an image to WebP format with configurable quality and return metadata for the new file.

**Parameters:**
- `imageFilePath` (string): Fully qualified path to the image file.
- `quality` (int, optional): WebP compression quality (0-100, default 90).

**Returns:**  
`ConvertedImageMetadata` object, including the new file's metadata and estimated energy saved.

**Example Usage:**
``` json
{ ""tool"": ""convert_image_to_webp"", ""params"": { ""imageFilePath"": ""C:\images\sample.jpg"", ""quality"": 85 } }
```
---

## Resources

### 1. `ReadMe Text Resource`

- **URI:** `test://direct/text/resource`
- **Type:** `text/plain`
- **Description:** A direct text resource for demonstration.

### 2. `Template Resource`

- **URI Template:** `test://template/resource/{id}`
- **Description:** A template resource with a numeric ID, returns either text or blob content based on the resource.

---

## Error Handling

- If a file does not exist, a descriptive exception is thrown.
- Invalid parameters (e.g., out-of-range quality) result in protocol errors.
- Metadata extraction failures for converted files raise an error.

---

## Integration Notes

- All tools and resources are exposed via MCP server tool/resource attributes.
- Use the provided URIs and tool names when constructing requests.
- Ensure file paths are accessible to the server process.";


    [McpServerResource(UriTemplate = "test://direct/text/resource", Name = "Repository Link", MimeType = "text/markdown")]
    [Description("A direct text resource linking to the GitHub repository.")]
    public static string RepositoryLinkResource() =>
        @"
# MCP Image Optimizer Repository

Find the source code and further documentation on [GitHub](https://github.com/johniwasz/mcp-image-optimizer).";
}
