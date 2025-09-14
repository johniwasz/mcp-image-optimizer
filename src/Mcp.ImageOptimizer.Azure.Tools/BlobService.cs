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
using Mcp.ImageOptimizer.Common;
using ModelContextProtocol.Server;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using System.ComponentModel;
using System.Globalization;

namespace Mcp.ImageOptimizer.Azure.Tools
{


    public class BlobService : IBlobService
    {
        private IImageConversationService _imageService;

        private IAzureResourceService _azureResourceService;

        public BlobService(IAzureResourceService azureResourceService, IImageConversationService imageService)
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

        public async Task UploadBlobAsync(BlobContainerClient blobContainer, string newBlobName, MemoryStream newBlob)
        {
            newBlob.Position = 0;
            var blobClient = blobContainer.GetBlobClient(newBlobName);
            await blobClient.UploadAsync(newBlob, overwrite: true);
        }

        public async Task<MemoryStream> DownloadBlobAsync(string accountName, string containerName, string blobName)
        {
            Uri blobUri = BuildBlobrUri(accountName, containerName, blobName);

            var credential = _azureResourceService.GetCredential();

            var blobClient = new BlobClient(blobUri, credential);

            var memoryStream = new MemoryStream();
            await blobClient.DownloadToAsync(memoryStream);
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


        private async Task<MemoryStream> DownloadBlobAsync(BlobContainerClient sourceContainerClient, string blobName)
        {
            var blobClient = sourceContainerClient.GetBlobClient(blobName);
            var memoryStream = new MemoryStream();
            await blobClient.DownloadToAsync(memoryStream);
            memoryStream.Position = 0; // Reset stream position after download
            return memoryStream;
        }

        private async Task<ConvertedImageMetadata> ConvertToWebPAsync(MemoryStream blobStream, string blobName, int quality = 80)
        {
            long originalImageSize = blobStream.Length;

            var filenameWithoutExtension = Path.GetFileNameWithoutExtension(blobName);

            // Load the image and save as WebP
            using (var image = await Image.LoadAsync<Rgba32>(blobStream))
            {
                var encoder = new WebpEncoder()
                {
                    Quality = quality
                };
            }

            // Get metadata for the new WebP file
            ImageMetadata imageData = await _imageService.GetImageMetadataFromFileAsync(blobName) ?? new ImageMetadata();

            ConvertedImageMetadata convertedMetadata = new ConvertedImageMetadata(imageData);

            long bytesSaved = originalImageSize - convertedMetadata.Size;
            convertedMetadata.EnergySaved = bytesSaved / ImageMetadata.GIGABYTES * 0.81;

            return convertedMetadata;

        }
        public async Task<IEnumerable<StorageAccountInfo>> ListStorageAccountsAsync(string? region = null, string? subscriptionId = null)
        {
            // Authenticate using DefaultAzureCredential (supports Azure CLI, Managed Identity, environment vars, Visual Studio, etc.)
            var credential = _azureResourceService.GetCredential();

            var armClient = new ArmClient(credential);

            SubscriptionResource? subscriptionResource = await _azureResourceService.GetSubscriptionResourceAsync(armClient, subscriptionId);

            if (subscriptionResource == null)
            {
                throw new InvalidOperationException("No Azure subscription could be resolved.");
            }

            var results = new List<StorageAccountInfo>(capacity: 16);

            // Iterate storage accounts in the subscription and filter by region if specified
            var storageAccounts = subscriptionResource.GetStorageAccountsAsync();
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

        public async Task<ImageMetadata> GetImageMetadataAsync(string storageAccountName, string containerName, string blobName)
        {
            var blobMem = await DownloadBlobAsync(storageAccountName, containerName, blobName);
            return await _imageService.GetImageMetadataFromStreamAsync(blobMem, blobName);

        }

        public async Task<IEnumerable<ConvertedImageMetadata>> ConvertImageAndGetMetadataAsync(string storageAccountName, int quality, bool deleteOriginal)
        {
            List<ConvertedImageMetadata> imageInfos = new List<ConvertedImageMetadata>();

            // Use data-plane BlobServiceClient with AAD credential to enumerate containers
            var blobServiceUri = new Uri($"https://{storageAccountName}.blob.core.windows.net");

            var blobServiceClient = new BlobServiceClient(blobServiceUri, _azureResourceService.GetCredential());

            await foreach (var containerItem in blobServiceClient.GetBlobContainersAsync())
            {
                try
                {
                    var containerClient = blobServiceClient.GetBlobContainerClient(containerItem.Name);

                    await foreach (var blobItem in containerClient.GetBlobsAsync())
                    {
                        var blobMem = await DownloadBlobAsync(storageAccountName, containerItem.Name, blobItem.Name);

                        long origSize = blobItem.Properties.ContentLength ?? 0;

                        var webPStream = await _imageService.ConvertToWebPAsync(blobMem, quality);

                        string newWebPName = $"{Path.GetFileNameWithoutExtension(blobItem.Name)}.webp";

                        ImageMetadata convertedMetadata = await _imageService.GetImageMetadataFromStreamAsync(webPStream, newWebPName);

                        await UploadBlobAsync(containerClient, newWebPName, webPStream);

                        ConvertedImageMetadata convertedImageMetadata = new ConvertedImageMetadata(convertedMetadata);

                        long bytesSaved = origSize - convertedMetadata.Size;

                        convertedImageMetadata.EnergySaved = (bytesSaved / ImageMetadata.GIGABYTES) * 0.81;

                        imageInfos.Add(convertedImageMetadata);

                        if (deleteOriginal)
                        {
                            var blobClient = containerClient.GetBlobClient(blobItem.Name);
                            await blobClient.DeleteIfExistsAsync();
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
    }
}
