using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Mcp.ImageOptimizer.Azure.Tools;
using Mcp.ImageOptimizer.Azure.Tools.Models;
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
    public async Task<IEnumerable<StorageAccountInfo>> ListStorageAccountsAync(
        IMcpServer server,
        IAzureResourceService azureResourceService,
        RequestContext<CallToolRequestParams> context,
        CancellationToken cancellationToken = default,
        [Description("Azure region")] string? region = null,
        [Description("Azure subscription")] string? subscriptionId = null)
    {
        // Authenticate using DefaultAzureCredential (supports Azure CLI, Managed Identity, environment vars, Visual Studio, etc.)
        var credential = azureResourceService.GetCredential();

        var armClient = new ArmClient(credential);

        SubscriptionResource? subscriptionResource = null;

        try
        {
            if (!string.IsNullOrWhiteSpace(subscriptionId))
            {
                var subId = subscriptionId.Trim();
                var subResourceId = new ResourceIdentifier($"/subscriptions/{subId}");
                subscriptionResource = armClient.GetSubscriptionResource(subResourceId);
                // Ensure the resource exists by calling GetAsync (will throw if not found / not authorized)
                await subscriptionResource.GetAsync(cancellationToken);
            }
            else
            {
                // Try to get the default subscription. Fall back to the first available subscription.
                try
                {
                    subscriptionResource = await armClient.GetDefaultSubscriptionAsync(cancellationToken);
                }
                catch (RequestFailedException)
                {
                    await foreach (var sub in armClient.GetSubscriptions().GetAllAsync(cancellationToken))
                    {
                        subscriptionResource = sub;
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Unable to resolve Azure subscription. Ensure you are authenticated and have access to a subscription.", ex);
        }

        if (subscriptionResource == null)
        {
            throw new InvalidOperationException("No Azure subscription could be resolved.");
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
                // Use data-plane BlobServiceClient with AAD credential to enumerate containers
                var blobServiceUri = new Uri($"https://{sa.Data.Name}.blob.core.windows.net");
                var blobServiceClient = new BlobServiceClient(blobServiceUri, credential);

                List<ContainerInfo> containerInfos = [];

                await foreach (var containerItem in blobServiceClient.GetBlobContainersAsync(cancellationToken: cancellationToken))
                {
                    ContainerInfo containerInfo = new ContainerInfo(containerItem.Name, containerItem.Properties.ETag.ToString(), containerItem.Properties.LastModified);

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

                    containerInfos.Add(containerInfo);
                }

                accountInfo.Containers = containerInfos;
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
            // Use data-plane BlobServiceClient with AAD credential to enumerate containers
            var blobServiceUri = new Uri($"https://{storageAccount.Data.Name}.blob.core.windows.net");

            var blobServiceClient = new BlobServiceClient(blobServiceUri, azureResourceService.GetCredential());

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
    IMcpServer server,
    IBlobService blobService,
    IAzureResourceService azureResourceService,
    RequestContext<CallToolRequestParams> context,
    [Description("Azure Storage Account name")] string storageAccountName,
    [Description("Quality of the converted image from 0 to 100")] int quality = 80,
    [Description("Indictect if the original image should be deleted")] bool deleteOriginal = false,
    [Description("Azure subcription id")] string? subscriptionId = null,
    CancellationToken cancellationToken = default)
    {
        List<ConvertedImageMetadata> imageInfos = new List<ConvertedImageMetadata>();

        // Authenticate using DefaultAzureCredential (supports Azure CLI, Managed Identity, environment vars, Visual Studio, etc.)

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



}

