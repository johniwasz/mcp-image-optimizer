// Global using directives for Mcp.ImageOptimizer.StreamingHttp

// System namespaces
global using System;
global using System.Collections.Generic;
global using System.ComponentModel;
global using System.Linq;
global using System.Text;
global using System.Threading;
global using System.Threading.Tasks;

// Azure namespaces
global using Azure;
global using Azure.Core;
global using Azure.Identity;
global using Azure.ResourceManager;
global using Azure.ResourceManager.Resources;
global using Azure.ResourceManager.Storage;
global using Azure.Storage.Blobs;
global using Azure.Storage.Blobs.Models;

// Microsoft Extensions
global using Microsoft.Extensions.Azure;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;

// ASP.NET Core
global using Microsoft.AspNetCore.Builder;

// Model Context Protocol
global using ModelContextProtocol;
global using ModelContextProtocol.Protocol;
global using ModelContextProtocol.Server;
global using ModelContextProtocol.AspNetCore;

// Microsoft Extensions AI
global using Microsoft.Extensions.AI;

// Project-specific namespaces
global using Mcp.ImageOptimizer.Azure.Services;
global using Mcp.ImageOptimizer.Azure.Services.Models;
global using Mcp.ImageOptimizer.Common;
global using Mcp.ImageOptimizer.Common.Models;
