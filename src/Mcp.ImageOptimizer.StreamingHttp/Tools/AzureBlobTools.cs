using System.Text.Json;
using static ModelContextProtocol.Protocol.ElicitRequestParams;

namespace Mcp.ImageOptimizer.StreamingHttp.Tools;

[McpServerToolType]
internal class AzureBlobTools
{
    [McpServerTool(Name = "list_blob_image_metadata", ReadOnly = true, Title = "Get a list of image blob metadata")]
    [Description("Retrieves a list of Azure storage accounts, containers, and blobs in a region or subscription.")]
    public async Task<IEnumerable<ImageMetadata>> GetBlobImageInfoAsyc(
           McpServer server,
           IBlobService blobService,
           IAzureResourceService azureResourceService,
           RequestContext<CallToolRequestParams> context,
           [Description("Azure Storage Account name")] string? storageAccountName = null,
           [Description("Azure subscription id")] string? subscriptionId = null,
           CancellationToken cancellationToken = default)
    {
        List<ImageMetadata> imageInfos = new List<ImageMetadata>();

        var logger = server.AsClientLoggerProvider().CreateLogger("AzureBlobTools");
        
        if(string.IsNullOrWhiteSpace(storageAccountName))
        {
            storageAccountName = await ElicitStorageAccount(server, storageAccountName, logger, false, cancellationToken);
        }

        var storageAccount = await azureResourceService.GetStorageAccountResourceAsync(storageAccountName, subscriptionId);

        if(storageAccount == null)
        {
            logger.LogError($"Storage account '{storageAccountName}' could not be found.");

            storageAccountName = await ElicitStorageAccount(server, storageAccountName, logger, false, cancellationToken);
            if (string.IsNullOrEmpty(storageAccountName))
            {
                throw new McpException($"{nameof(storageAccountName)} cannot be null", McpErrorCode.InvalidParams);
            }
            else
            {
                storageAccount = await azureResourceService.GetStorageAccountResourceAsync(storageAccountName, subscriptionId);
                if (storageAccount == null)
                {
                    throw new InvalidOperationException($"Storage account '{storageAccountName}' could not be found.");
                }
            }
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
                catch (RequestFailedException ex)
                {
                    // Could not enumerate blobs for this container (permissions/network) - continue with empty list
                    logger.LogWarning(ex, "Could not enumerate blobs for container '{ContainerName}' in storage account '{StorageAccountName}'", containerItem.Name, storageAccountName);
                }
            }
        }
        catch (RequestFailedException ex)
        {
            // If we cannot list containers for this account (lack of permissions or network), leave the list empty.
            logger.LogError(ex, "Error accessing storage account '{StorageAccountName}'", storageAccountName); 
        }
        catch (Exception ex)
        {
            // Swallow other errors per-account to avoid failing the entire operation. Optionally log.
            logger.LogError(ex, "Unexpected error processing storage account '{StorageAccountName}'", storageAccountName);
        }

        return imageInfos;
    }

    [McpServerTool(Name = "shrink_blob_images", ReadOnly = false, Title = "Shrink Blob Images")]
    [Description("Convert blob images to a smaller format (WebP). The original image can be optionally deleted.")]
    public async Task<IEnumerable<ConvertedImageMetadata>> ShrinkBlobImagesAsyc(
        IBlobService blobService,
        McpServer server,
        IAzureResourceService azureResourceService,
        [Description("Azure Storage Account name")] string? storageAccountName = null,
        [Description("Quality of the converted image from 0 to 100")] int quality = 80,
        [Description("Indicate if the original image should be deleted")] bool deleteOriginal = false,
        [Description("Azure subscription id")] string? subscriptionId = null,
        CancellationToken cancellationToken = default)
    {
        List<ConvertedImageMetadata> imageInfos = new();

        var logger = server.AsClientLoggerProvider().CreateLogger("AzureBlobTools");
        storageAccountName = await ElicitStorageAccount(server, storageAccountName, logger, true, cancellationToken);

        var storageAccount = await azureResourceService.GetStorageAccountResourceAsync(storageAccountName, subscriptionId, cancellationToken);

        if (storageAccount == null)
        {
            logger.LogError("Storage account '{StorageAccountName}' could not be found.", storageAccountName);
            throw new McpException($"Storage account '{storageAccountName}' could not be found.", McpErrorCode.InvalidParams);
        }

        try
        {
            imageInfos.AddRange(await blobService.ConvertImageAndGetMetadataAsync(storageAccountName, quality, deleteOriginal, cancellationToken));
        }
        catch (RequestFailedException ex)
        {
            logger.LogError(ex, "Error converting images in storage account '{StorageAccountName}'", storageAccountName);
            // If we cannot list containers for this account (lack of permissions or network), leave the list empty.
        } 
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error converting images in storage account '{StorageAccountName}'", storageAccountName);
            // Swallow other errors per-account to avoid failing the entire operation. Optionally log.
        }

        return imageInfos;
    }

    private static async Task<string?> ElicitStorageAccount(McpServer server, string? storageAccountName, ILogger logger, bool includeAllStorageCheck, CancellationToken cancellationToken)
    {
      
        if (string.IsNullOrWhiteSpace(storageAccountName) && server.ClientCapabilities?.Elicitation != null)
        {
            bool includeAllAccounts = false;

            if (includeAllStorageCheck)
            {
                // Use elicitation to ask for the storage account name if not provided
                // First ask the user if they want to play
                var allStorageAccountsSchema = new RequestSchema
                {
                    Properties =
            {
                ["Answer"] = new BooleanSchema()
            }
                };

                var allAccountsResponse = await server.ElicitAsync(new ElicitRequestParams
                {
                    Message = "Would you like to retrieve blobs from all storage accounts? This could take a while",
                    RequestedSchema = allStorageAccountsSchema
                }, cancellationToken);


                includeAllAccounts = allAccountsResponse.IsAccepted && allAccountsResponse.Content?["Answer"].ValueKind == JsonValueKind.True;
            }

            // Check if user wants to play
            if (includeAllAccounts)
            {
                // Ask for the storage account name
                var accountSchemaRequest = new RequestSchema
                {
                    Properties =
                {
                    ["StorageAccountName"] = new StringSchema()
                    {
                        Title = "Storage Account Name",
                        Description = "The name of the Azure Storage Account to process",
                        MinLength = 3,
                        MaxLength = 24,
                    }
                },
                    Required = ["StorageAccountName"]
                };

                var storageAccountResponse = await server.ElicitAsync(new ElicitRequestParams
                {
                    Message = "Provide the name of the storage account.",
                    RequestedSchema = accountSchemaRequest
                }, cancellationToken);

                if (storageAccountResponse.IsAccepted && storageAccountResponse.Content?["StorageAccountName"].ValueKind == JsonValueKind.String)
                {
                    storageAccountName = storageAccountResponse.Content?["StorageAccountName"].GetString()!;
                }
            }
            else
            {
                logger.LogTrace("User opted not to search for a storage account");
                // Continue searching for all.
                storageAccountName = null;
            }

        }

        return storageAccountName;
    }

    [McpServerTool(Name = "list_deleted_large_blob_images", ReadOnly = true, Title = "List Deleted Large Images")]
    [Description("List all deleted large blob images by container. Returns container information with a list of deleted blobs per container.")]
    public async Task<IEnumerable<ContainerInfo>> ListDeletedLargeImageBlobsAsync(
        IBlobService blobService, 
        McpServer server,
        [Description("Azure storage account name")] string storageAccountName, 
        CancellationToken cancellationToken = default)
    {
        var logger = server.AsClientLoggerProvider().CreateLogger("AzureBlobTools");

        if (string.IsNullOrWhiteSpace(storageAccountName))
        {
            logger.LogError("Storage Account Name cannot be null or empty");
            throw new McpException("Storage Account Name cannot be null or empty", McpErrorCode.InvalidParams);
        }

        try
        {
            return await blobService.ListDeletedImageBlobsAsync(storageAccountName, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing deleted image blobs for storage account '{StorageAccountName}'", storageAccountName);
            throw;
        }
    }

    [McpServerTool(Name = "restore_deleted_large_blob_images", ReadOnly = false, Title = "Restore Deleted Large Images")]
    [Description("Restore (undelete) all deleted large blob images. Returns container information with a list of restored blobs per container.")]
    public async Task<IEnumerable<ContainerInfo>> RestoreLargeImageDeletedBlobs(
        IBlobService blobService,
        McpServer server,
        [Description("Azure storage account name")] string storageAccountName,
        CancellationToken cancellationToken = default)
    {
        var logger = server.AsClientLoggerProvider().CreateLogger("AzureBlobTools");

        if (string.IsNullOrWhiteSpace(storageAccountName))
        {
            logger.LogError("Storage Account Name cannot be null or empty");
            throw new McpException("Storage Account Name cannot be null or empty", McpErrorCode.InvalidParams);
        }

        try
        {
            return await blobService.RestoreDeletedImageBlobsAsync(storageAccountName, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error restoring deleted image blobs for storage account '{StorageAccountName}'", storageAccountName);
            throw;
        }
    }
}

