using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Mcp.ImageOptimizer.Azure.Services;
using Mcp.ImageOptimizer.Azure.Services.Models;
using Mcp.ImageOptimizer.Common;
using Mcp.ImageOptimizer.Common.Models;
using ModelContextProtocol.Server;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using System.ComponentModel;
using System.Globalization;

namespace Mcp.ImageOptimizer.Azure.Services;

public class BlobService : IBlobService
{
    private IImageConversionService _imageService;

    private IAzureResourceService _azureResourceService;

    public BlobService(IAzureResourceService azureResourceService, IImageConversionService imageService)
    {
        _azureResourceService = azureResourceService;
        _imageService = imageService;
    }
    /*
     *
    public static async Task ConvertBlobAsync(string sourceConnectionString, string sourceContainerName, string sourceBlobName, string destinationConnectionString, string destinationContainerName, string destinationBlobName)
    {
        // Placeholder for future implementation
        
        BlobContainerClient sourceContainerClient = new BlobContainerClient(sourceConnectionString, sourceContainerName);

        using MemoryStream blobStream = await DownloadBlobByConnectionStringAsync(sourceContainerClient, sourceBlobName);

        // Here you would add the image conversion logic (e.g., using ImageSharp)
        BlobContainerClient destinationContainerClient = new BlobContainerClient(destinationConnectionString, destinationContainerName);
        var destinationBlobClient = destinationContainerClient.GetBlobClient(destinationBlobName);
       
    }

    public static async Task<MemoryStream> DownloadBlobByConnectionStringAsync(BlobContainerClient sourceContainerClient, string containerName, string blobName)
    {

        var blobClient = new BlobClient(sourceContainerClient, containerName, blobName);

        var memoryStream = new MemoryStream();
        await blobClient.DownloadToAsync(memoryStream);
        memoryStream.Position = 0; // Reset stream position after download
        return memoryStream;
    }
    */

    public async Task UploadBlobAsync(BlobContainerClient blobContainer, string newBlobName, MemoryStream newBlob, CancellationToken cancellationToken = default)
    {
        newBlob.Position = 0;
        var blobClient = blobContainer.GetBlobClient(newBlobName);
        await blobClient.UploadAsync(newBlob, overwrite: true, cancellationToken: cancellationToken);
    }

    public async Task<MemoryStream> DownloadBlobAsync(string accountName, string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        Uri blobUri = BuildBlobrUri(accountName, containerName, blobName);

        var credential = _azureResourceService.GetCredential();

        var blobClient = new BlobClient(blobUri, credential);

        var memoryStream = new MemoryStream();
        await blobClient.DownloadToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0; // Reset stream position after download
        return memoryStream;
    }

    private Uri BuildContainerUri(string accountName, string containerName)
    {
        // Replace "blob.core.windows.net" with the correct endpoint for your region if necessary
        // For example, "blob.core.chinacloudapi.cn" for Azure China

        string uriString = $"https://{accountName}.blob.core.windows.net/{containerName}";
        return new Uri(uriString);
    }

    private Uri BuildBlobrUri(string accountName, string containerName, string blobName)
    {
        // Replace "blob.core.windows.net" with the correct endpoint for your region if necessary
        // For example, "blob.core.chinacloudapi.cn" for Azure China
        string uriString = $"https://{accountName}.blob.core.windows.net/{containerName}/{blobName}";
        return new Uri(uriString);
    }


    private async Task<MemoryStream> DownloadBlobAsync(BlobContainerClient sourceContainerClient, string blobName, CancellationToken cancellationToken = default)
    {
        var blobClient = sourceContainerClient.GetBlobClient(blobName);
        var memoryStream = new MemoryStream();
        await blobClient.DownloadToAsync(memoryStream, cancellationToken: cancellationToken);
        memoryStream.Position = 0; // Reset stream position after download
        return memoryStream;
    }

    private async Task<ConvertedImageMetadata> ConvertToWebPAsync(MemoryStream blobStream, string blobName, int quality = 80, CancellationToken cancellationToken = default)
    {
        long originalImageSize = blobStream.Length;

        var filenameWithoutExtension = Path.GetFileNameWithoutExtension(blobName);

        // Load the image and save as WebP
        using (var image = await Image.LoadAsync<Rgba32>(blobStream, cancellationToken))
        {
            var encoder = new WebpEncoder()
            {
                Quality = quality
            };
        }

        // Get metadata for the new WebP file
        ImageMetadata imageData = await _imageService.GetImageMetadataFromFileAsync(blobName, cancellationToken) ?? new ImageMetadata();

        ConvertedImageMetadata convertedMetadata = new ConvertedImageMetadata(imageData);

        long bytesSaved = originalImageSize - convertedMetadata.Size;
        convertedMetadata.EnergySaved = bytesSaved / ImageMetadata.GIGABYTES * 0.81;

        return convertedMetadata;

    }
    public async Task<IEnumerable<StorageAccountInfo>> ListStorageAccountsAsync(string? region = null, string? subscriptionId = null, CancellationToken cancellationToken = default)
    {
        // Authenticate using DefaultAzureCredential (supports Azure CLI, Managed Identity, environment vars, Visual Studio, etc.)
        var credential = _azureResourceService.GetCredential();

        var armClient = new ArmClient(credential);

        SubscriptionResource? subscriptionResource = await _azureResourceService.GetSubscriptionResourceAsync(armClient, subscriptionId, cancellationToken);

        if (subscriptionResource == null)
        {
            throw new InvalidOperationException("No Azure subscription could be resolved.");
        }

        var results = new List<StorageAccountInfo>(capacity: 16);

        // Iterate storage accounts in the subscription and filter by region if specified
        var storageAccounts = subscriptionResource.GetStorageAccountsAsync(cancellationToken);
        await foreach (var sa in storageAccounts)
        {
            var loc = sa.Data.Location.Name ?? sa.Data.Location.ToString() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(region) &&
                !string.Equals(loc, region, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(sa.Data.Location.ToString(), region, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            results.Add(new StorageAccountInfo(
                Name: sa.Data.Name,
                Id: sa.Data.Id.ToString(),
                Location: loc,
                Kind: sa.Data.Kind?.ToString() ?? string.Empty,
                Sku: sa.Data.Sku.Name.ToString() ?? string.Empty
            ));
        }

        return results;
    }

    public async Task<ImageMetadata> GetImageMetadataAsync(string storageAccountName, string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        var blobMem = await DownloadBlobAsync(storageAccountName, containerName, blobName, cancellationToken);
        return await _imageService.GetImageMetadataFromStreamAsync(blobMem, blobName, cancellationToken);

    }

    public async Task<IEnumerable<ConvertedImageMetadata>> ConvertImageAndGetMetadataAsync(
        string storageAccountName, int quality, bool deleteOriginal, CancellationToken cancellationToken = default)
    {
        List<ConvertedImageMetadata> imageInfos = new List<ConvertedImageMetadata>();

        var blobServiceClient = _azureResourceService.GetBlobServiceClient(storageAccountName); 

        await foreach (var containerItem in blobServiceClient.GetBlobContainersAsync(cancellationToken: cancellationToken))
        {
            try
            {
                var containerClient = blobServiceClient.GetBlobContainerClient(containerItem.Name);

                await foreach (var blobItem in containerClient.GetBlobsAsync())
                {
                    // Detemine if the blob is an image we can process
                    if (_imageService.IsLargeImageMimeType(blobItem.Properties.ContentType))
                    {
                        var blobMem = await DownloadBlobAsync(storageAccountName, containerItem.Name, blobItem.Name, cancellationToken);

                        long origSize = blobItem.Properties.ContentLength ?? 0;

                        var webPStream = await _imageService.ConvertToWebPAsync(blobMem, quality, cancellationToken);

                        string newWebPName = $"{Path.GetFileNameWithoutExtension(blobItem.Name)}.webp";

                        ImageMetadata convertedMetadata = await _imageService.GetImageMetadataFromStreamAsync(webPStream, newWebPName, cancellationToken);

                        await UploadBlobAsync(containerClient, newWebPName, webPStream);

                        ConvertedImageMetadata convertedImageMetadata = new(convertedMetadata);

                        long bytesSaved = origSize - convertedMetadata.Size;

                        convertedImageMetadata.EnergySaved = (bytesSaved / ImageMetadata.GIGABYTES) * 0.81;

                        imageInfos.Add(convertedImageMetadata);

                        if (deleteOriginal)
                        {
                            var blobClient = containerClient.GetBlobClient(blobItem.Name);
                            await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
                        }
                    }
                    else
                    {
                        ImageMetadata imageMetadata = new()
                        {
                            Path = blobItem.Name,
                            Size = blobItem.Properties.ContentLength ?? 0
                        };

                        imageMetadata.ExifData.Add("Comment", "Not an inefficient image format");

                        imageInfos.Add(new ConvertedImageMetadata(imageMetadata));
                    }
                }
            }
            catch (RequestFailedException)
            {
                // Could not enumerate blobs for this container (permissions/network) - continue with empty list
            } 
        }
        return imageInfos;
    }

    public async Task<IEnumerable<ContainerInfo>> ListDeletedImageBlobsAsync(string storageAccountName, CancellationToken cancellationToken = default)
    {
        var deletedImageBlobs = new List<ContainerInfo>();
        var blobServiceClient = _azureResourceService.GetBlobServiceClient(storageAccountName);

        await foreach (var containerItem in blobServiceClient.GetBlobContainersAsync(cancellationToken: cancellationToken))
        {
            try
            {
                ContainerInfo containerInfo = new(
                    containerItem.Name,
                    containerItem.Properties.ETag.ToString(),
                    containerItem.Properties.LastModified);

                var containerClient = blobServiceClient.GetBlobContainerClient(containerItem.Name);

                List<string> blobs = new();

                // List deleted blobs in the container
                await foreach (var blobItem in containerClient.GetBlobsAsync(BlobTraits.None, BlobStates.Deleted, null, cancellationToken))
                {
                    // Check if the blob is an image based on its content type
                    if (_imageService.IsLargeImageMimeType(blobItem.Properties.ContentType) && blobItem.Deleted)
                    {
                        blobs.Add(blobItem.Name);
                    }
                }

                if (blobs.Count > 0)
                {
                    containerInfo.Blobs = blobs;
                }

                deletedImageBlobs.Add(containerInfo);
            }
            catch (RequestFailedException)
            {
                // Could not enumerate blobs for this container (permissions/network) - continue with next container
            }
        }

        return deletedImageBlobs;
    }

    public async Task<IEnumerable<ContainerInfo>> RestoreDeletedImageBlobsAsync(string storageAccountName, CancellationToken cancellationToken = default)
    {
        var restoredBlobNames = new List<ContainerInfo>();
        var blobServiceClient = _azureResourceService.GetBlobServiceClient(storageAccountName);

        await foreach (var containerItem in blobServiceClient.GetBlobContainersAsync(cancellationToken: cancellationToken))
        {
            try
            {
                ContainerInfo containerInfo = new(
                    containerItem.Name, 
                    containerItem.Properties.ETag.ToString(), 
                    containerItem.Properties.LastModified);

                var containerClient = blobServiceClient.GetBlobContainerClient(containerItem.Name);

                List<string> blobs = new();

                // List deleted blobs in the container
                await foreach (var blobItem in containerClient.GetBlobsAsync(BlobTraits.None, BlobStates.Deleted, null, cancellationToken))
                {
                    // Check if the blob is an image based on its content type
                    if (_imageService.IsLargeImageMimeType(blobItem.Properties.ContentType))
                    {
                        if (blobItem.Deleted)
                        {
                            try
                            {
                                var blobClient = containerClient.GetBlobClient(blobItem.Name);
                                await blobClient.UndeleteAsync(cancellationToken);
                                blobs.Add(blobItem.Name);
                            }
                            catch (RequestFailedException)
                            {
                                // Failed to restore this specific blob - continue with others
                            }
                        }
                    }
                }

                if (blobs.Count > 0)
                {
                    containerInfo.Blobs = blobs;
                }

                restoredBlobNames.Add(containerInfo);
            }
            catch (RequestFailedException)
            {
                // Could not enumerate blobs for this container (permissions/network) - continue with next container
            }
        }

        return restoredBlobNames;
    }
}
