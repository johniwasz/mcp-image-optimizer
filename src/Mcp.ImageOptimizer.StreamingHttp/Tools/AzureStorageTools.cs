using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Mcp.ImageOptimizer.Azure.Services;
using Mcp.ImageOptimizer.Azure.Services.Models;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Azure;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Mcp.ImageOptimizer.Common.Models;
using Azure;
using Microsoft.Extensions.Logging;

namespace Mcp.ImageOptimizer.StreamingHttp.Tools
{
    internal class AzureStorageTools
    {

        [McpServerTool(Name = "list_storage_accounts", ReadOnly = true, Title = "List all Azure Storage Accounts")]
        [Description("Retrieves a list of Azure storage accounts, containers, and blobs in a region or subscription.")]
        internal async Task<IEnumerable<StorageAccountInfo>> ListStorageAccountsAync(
        McpServer server,
        IAzureResourceService azureResourceService,
        RequestContext<CallToolRequestParams> context,
        [Description("Azure region")] string? region = null,
        [Description("Azure subscription")] string? subscriptionId = null,
        [Description("Indicates if blobs should be included in the results")] bool includeBlobs = false,
        CancellationToken cancellationToken = default)
        {
            SubscriptionResource? subscriptionResource = null;

            var logger = server.AsClientLoggerProvider().CreateLogger("AzureBlobTools");

            try
            {
                subscriptionResource = await azureResourceService.GetSubscriptionResourceAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unable to resolve Azure subscription. Ensure you are authenticated and have access to a subscription.");
                throw new McpException("Unable to resolve Azure subscription. Ensure you are authenticated and have access to a subscription.", ex);
            }

            if (subscriptionResource == null)
            {
                logger.LogError("No Azure subscription could be resolved.");
                throw new McpException("No Azure subscription could be resolved.");
            }

            var results = new List<StorageAccountInfo>(capacity: 16);

            var progressToken = context.Params?.ProgressToken;

            // Iterate storage accounts in the subscription and filter by region if specified
            var storageAccounts = subscriptionResource.GetStorageAccountsAsync(cancellationToken);
            int index = 0;
            await foreach (var sa in storageAccounts)
            {
                index++;

                var loc = sa.Data.Location.Name ?? sa.Data.Location.ToString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(region) &&
                    !string.Equals(loc, region, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(sa.Data.Location.ToString(), region, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                StorageAccountInfo accountInfo = new(
                    Name: sa.Data.Name,
                    Id: sa.Data.Id.ToString(),
                    Location: loc,
                    Kind: sa.Data.Kind?.ToString() ?? string.Empty,
                    Sku: sa.Data.Sku.Name.ToString() ?? string.Empty
                );

                try
                {
                    var blobServiceClient = azureResourceService.GetBlobServiceClient(sa.Data.Name);
                    List<ContainerInfo> containerInfos = [];

                    await foreach (var containerItem in blobServiceClient.GetBlobContainersAsync(cancellationToken: cancellationToken))
                    {
                        ContainerInfo containerInfo = new ContainerInfo(containerItem.Name, containerItem.Properties.ETag.ToString(), containerItem.Properties.LastModified);

                        if (includeBlobs)
                        {
                            var blobNames = new List<string>();

                            try
                            {
                                var containerClient = blobServiceClient.GetBlobContainerClient(containerItem.Name);

                                await foreach (var blobItem in containerClient.GetBlobsAsync(cancellationToken: cancellationToken))
                                {
                                    blobNames.Add(blobItem.Name);
                                }
                            }
                            catch (RequestFailedException ex)
                            {
                                logger.LogWarning(ex, "Could not enumerate blobs for container '{ContainerName}' in storage account '{StorageAccountName}'", containerItem.Name, sa.Data.Name);
                                // Could not enumerate blobs for this container (permissions/network) - continue with empty list
                            }

                            containerInfo.Blobs = blobNames;
                        }

                        containerInfos.Add(containerInfo);
                        accountInfo.Containers = containerInfos;
                    }
                }
                catch (RequestFailedException ex)
                {
                    logger.LogWarning(ex, "Could not list containers for storage account '{StorageAccountName}'", sa.Data.Name);
                    // If we cannot list containers for this account (lack of permissions or network), leave the list empty.
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unexpected error while processing storage account '{StorageAccountName}'", sa.Data.Name);
                    // Swallow other errors per-account to avoid failing the entire operation. Optionally log.
                }

                results.Add(accountInfo);

                await server.SendNotificationAsync("notifications/progress", new
                {
                    Progress = index,
                    StorageAccount = accountInfo,
                    progressToken
                });
            }

            return results;
        }
    }
}
