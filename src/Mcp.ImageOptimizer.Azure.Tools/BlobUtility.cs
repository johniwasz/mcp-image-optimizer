using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Storage;
using Azure.Storage.Blobs;
using Mcp.ImageOptimizer.Common;
using ModelContextProtocol.Server;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using System.ComponentModel;
using System.Globalization;

namespace Mcp.ImageOptimizer.Azure.Tools
{
    public class StorageAccountInfo
    {
       public StorageAccountInfo(string Name, string Id, string Location, string Kind, string Sku)
        {
            this.Name = Name;
            this.Id = Id;
            this.Location = Location;
            this.Kind = Kind;
            this.Sku = Sku;
        }

        public string Name { get; }
        public string Id { get; }
        public string Location { get; }
        public string Kind { get; }
        public string Sku { get; }

        public IEnumerable<ContainerInfo> Containers { get; set; } = Array.Empty<ContainerInfo>();    

    } 

    public class ContainerInfo
    {
        public ContainerInfo(string name, string etag, DateTimeOffset lastModified)
        {
            this.Name = name;
            this.Etag = etag;
            LastModified = lastModified;
        }

        public string Name { get; }
        public string Etag { get; }

        public DateTimeOffset LastModified { get; }

        public IEnumerable<string> Blobs { get; set; } = Array.Empty<string>();

    }

    public record BlobInfo(string Name, string Etag, DateTimeOffset LastModified, long Size);


    public class BlobUtility
    {
        /*
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

        public static async Task<MemoryStream> DownloadBlobAsync(string accountName, string containerName, string blobName)
        {
            Uri blobUri = BuildBlobrUri(accountName, containerName, blobName);

            var credential = AzureResourceUtility.GetCredential();

            var blobClient = new BlobClient(blobUri, credential);

            var memoryStream = new MemoryStream();
            await blobClient.DownloadToAsync(memoryStream);
            memoryStream.Position = 0; // Reset stream position after download
            return memoryStream;
        }

        private static Uri BuildContainerUri(string accountName, string containerName)
        {
            // Replace "blob.core.windows.net" with the correct endpoint for your region if necessary
            // For example, "blob.core.chinacloudapi.cn" for Azure China
            string blobEndpoint = "blob.core.windows.net";

            string uriString = $"https://{accountName}.blob.core.windows.net/{containerName}";
            return new Uri(uriString);
        }

        private static Uri BuildBlobrUri(string accountName, string containerName, string blobName)
        {
            // Replace "blob.core.windows.net" with the correct endpoint for your region if necessary
            // For example, "blob.core.chinacloudapi.cn" for Azure China
            string blobEndpoint = "blob.core.windows.net";

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
            ImageMetadata imageData = await ImageUtilities.GetImageMetadataFromFileAsync(blobName) ?? new ImageMetadata();

            ConvertedImageMetadata convertedMetadata = new ConvertedImageMetadata(imageData);
          
            long bytesSaved = originalImageSize - convertedMetadata.Size;
            convertedMetadata.EnergySaved = bytesSaved / ImageMetadata.GIGABYTES * 0.81;

            return convertedMetadata;

        }
        public async Task<IEnumerable<StorageAccountInfo>> ListStorageAccountsAsync(string region = null, string subscriptionId = null)
        {
            // Authenticate using DefaultAzureCredential (supports Azure CLI, Managed Identity, environment vars, Visual Studio, etc.)
            var credential = AzureResourceUtility.GetCredential();

            var armClient = new ArmClient(credential);

            SubscriptionResource subscriptionResource = await AzureResourceUtility.GetSubscriptionResourceAsync(armClient, subscriptionId);

            if (subscriptionResource == null)
            {
                throw new InvalidOperationException("No Azure subscription could be resolved.");
            }

            var results = new List<StorageAccountInfo>(capacity: 16);

            // Iterate storage accounts in the subscription and filter by region if specified
            var storageAccounts = subscriptionResource.GetStorageAccountsAsync();
            await foreach (var sa in storageAccounts)
            {
                // Replace this line:
                // await foreach (var sa in storageAccounts)
                
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
    }
}
