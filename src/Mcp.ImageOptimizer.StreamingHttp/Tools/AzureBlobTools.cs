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

namespace Mcp.ImageOptimizer.StreamingHttp.Tools;

[McpServerToolType]
internal class AzureBlobTools
{

    [McpServerTool(Name = "list_storage_accounts", ReadOnly = true, Title = "List all Azure Storage Accounts")]
    [Description("Retrieves a list of Azure storage accounts, containers, and blobs in a region or subscription.")]
    internal async Task<IEnumerable<StorageAccountInfo>> ListStorageAccountsAync(
        IMcpServer server,
        IAzureResourceService azureResourceService,
        RequestContext<CallToolRequestParams> context,
        [Description("Azure region")] string? region = null,
        [Description("Azure subscription")] string? subscriptionId = null,
        [Description("Indicates if blobs should be included in the results")] bool includeBlobs = false,
        CancellationToken cancellationToken = default)
    {
        SubscriptionResource? subscriptionResource = null;

        try
        {
            subscriptionResource = await azureResourceService.GetSubscriptionResourceAsync(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            throw new McpException("Unable to resolve Azure subscription. Ensure you are authenticated and have access to a subscription.", ex);
        }

        if (subscriptionResource == null)
        {
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

            StorageAccountInfo accountInfo = new StorageAccountInfo(
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
                        catch (RequestFailedException)
                        {
                            // Could not enumerate blobs for this container (permissions/network) - continue with empty list
                        }

                        containerInfo.Blobs = blobNames;
                    }

                    containerInfos.Add(containerInfo);

                    accountInfo.Containers = containerInfos;

                }
            }
            catch (RequestFailedException)
            {
                // If we cannot list containers for this account (lack of permissions or network), leave the list empty.
            }
            catch (Exception)
            {
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


    [McpServerTool(Name = "get_bloblist_image_metadata", ReadOnly = true, Title = "Get a list of image blob metadata")]
    [Description("Retrieves a list of Azure storage accounts, containers, and blobs in a region or subscription.")]
    public async Task<IEnumerable<ImageMetadata>> GetBlobImageInfoAsyc(
           IMcpServer server,
           IBlobService blobService,
           IAzureResourceService azureResourceService,
           RequestContext<CallToolRequestParams> context,
           [Description("Azure Storage Account name")] string storageAccountName,
           [Description("Azure subscription id")] string? subscriptionId = null,
           CancellationToken cancellationToken = default)
    {
        List<ImageMetadata> imageInfos = new List<ImageMetadata>();

        // Authenticate using DefaultAzureCredential (supports Azure CLI, Managed Identity, environment vars, Visual Studio, etc.)

        var storageAccount = await azureResourceService.GetStorageAccountResourceAsync(storageAccountName, subscriptionId);


        if(storageAccount == null)
        {
            throw new InvalidOperationException($"Storage account '{storageAccountName}' could not be found.");
        }

        try
        {
            var blobServiceClient = azureResourceService.GetBlobServiceClient(storageAccountName);

            List<ContainerInfo> containerInfos = new();

            await foreach (var containerItem in blobServiceClient.GetBlobContainersAsync(cancellationToken: cancellationToken))
            {
                try
                {
                    var containerClient = blobServiceClient.GetBlobContainerClient(containerItem.Name);

                    await foreach (var blobItem in containerClient.GetBlobsAsync(cancellationToken: cancellationToken))
                    {
                        imageInfos.Add(await blobService.GetImageMetadataAsync(storageAccountName, containerItem.Name, blobItem.Name));
                    }
                }
                catch (RequestFailedException)
                {
                    // Could not enumerate blobs for this container (permissions/network) - continue with empty list
                }
            }
        }
        catch (RequestFailedException)
        {
            // If we cannot list containers for this account (lack of permissions or network), leave the list empty.
        }
        catch (Exception)
        {
            // Swallow other errors per-account to avoid failing the entire operation. Optionally log.
        }

        return imageInfos;
    }

    [McpServerTool(Name = "shrink_blob_images", ReadOnly = false, Title = "Shrink Blob Images")]
    [Description("Convert blob images to a smaller format (WebP). The original image can be optionally deleted.")]
    public async Task<IEnumerable<ConvertedImageMetadata>> ShrinkBlobImagesAsyc(
        IBlobService blobService,
        IAzureResourceService azureResourceService,
        [Description("Azure Storage Account name")] string storageAccountName,
        [Description("Quality of the converted image from 0 to 100")] int quality = 80,
        [Description("Indicate if the original image should be deleted")] bool deleteOriginal = false,
        [Description("Azure subscription id")] string? subscriptionId = null,
        CancellationToken cancellationToken = default)
    {
        List<ConvertedImageMetadata> imageInfos = new();

        var storageAccount = await azureResourceService.GetStorageAccountResourceAsync(storageAccountName, subscriptionId, cancellationToken);

        if(storageAccount == null)
        {
            throw new McpException($"Storage account '{storageAccountName}' could not be found.", McpErrorCode.InvalidParams);
        }

        try
        {         
            imageInfos.AddRange(await blobService.ConvertImageAndGetMetadataAsync(storageAccountName, quality, deleteOriginal, cancellationToken));
        }
        catch (RequestFailedException)
        {
            // If we cannot list containers for this account (lack of permissions or network), leave the list empty.
        }
        catch (Exception)
        {
            // Swallow other errors per-account to avoid failing the entire operation. Optionally log.
        }

        return imageInfos;
    }

    [McpServerTool(Name = "long_running_operation"), Description("Demonstrates a long running operation with progress updates")]
    public async Task<string> LongRunningOperation(
        IMcpServer server,
        RequestContext<CallToolRequestParams> context,
        int duration = 10,
        int steps = 5)
    {
        var progressToken = context.Params?.ProgressToken;
        var stepDuration = duration / steps;

        for (int i = 1; i <= steps + 1; i++)
        {
            await Task.Delay(stepDuration * 1000);

            if (progressToken is not null)
            {
                await server.SendNotificationAsync("notifications/progress", new
                {
                    Progress = i,
                    Total = steps,
                    progressToken
                });
            }
        }

        return $"Long running operation completed. Duration: {duration} seconds. Steps: {steps}.";
    }

    [McpServerTool(Name = "list_deleted_blob_large_images", ReadOnly = true, Title = "List Deleted Large Images")]
    [Description("List all deleted large blob images by container. Returns container information with a list of deleted blobs per container.")]
    public async Task<IEnumerable<ContainerInfo>> ListDeletedLargeImageBlobsAsync(
        IBlobService blobService, 
        [Description("Azure strorage account name")] string storageAccountName, 
        CancellationToken cancellationToken = default)
    {
        if(string.IsNullOrWhiteSpace(storageAccountName))
        {
            throw new McpException("Storage Account Name cannot be null or empty", McpErrorCode.InvalidParams);
        }

        return await blobService.ListDeletedImageBlobsAsync(storageAccountName, cancellationToken);
    }

    [McpServerTool(Name = "restore_deleted_blob_large_images", ReadOnly = false, Title = "Restore Deleted Large Images")]
    [Description("Restore (undelete) all deleted large blob images. Returns container information with a list of restored blobs per container.")]
    public async Task<IEnumerable<ContainerInfo>> RestoreLargeImageDeletedBlobs(
        IBlobService blobService,
        [Description("Azure strorage account name")] string storageAccountName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageAccountName))
        {
            throw new McpException("Storage Account Name cannot be null or empty", McpErrorCode.InvalidParams);
        }

        return await blobService.RestoreDeletedImageBlobsAsync(storageAccountName, cancellationToken);
    }
}

