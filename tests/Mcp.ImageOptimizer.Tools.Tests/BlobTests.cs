using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Mcp.ImageOptimizer.Azure.Tools;
using Mcp.ImageOptimizer.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mcp.ImageOptimizer.Azure.Tools.Models;

namespace Mcp.ImageOptimizer.Tools.Tests
{
    public class BlobTests
    {

        private const string CONTAINER_NAME = "game-images";

        [Fact] 
        public async Task GetStorageAccountsAsync()
        {
            IAzureResourceService azureService = new AzureResourceService();
            IImageConversationService imageService = new ImageConversationService();
            IBlobService blobService = new BlobService(azureService, imageService);

            var accounts = await blobService.ListStorageAccountsAsync();
        }

        [Fact]
        public async Task CanReadBlobFromStorageAccountAync()
        {
            // Arrange
            string? connectionString = Environment.GetEnvironmentVariable("GAME_IMAGES_STORAGE");
            string containerName = CONTAINER_NAME;
            string blobName = "afi-welcome-lg.png";

            using var memoryStream = await DownloadBlobAsync(connectionString, containerName, blobName);
            // Assert
            Assert.True(memoryStream.Length > 0, "Blob content should not be empty.");
        }

        [Fact]
        public async Task ConvertAndUploadAync()
        {
            // Arrange
            string? connectionString = Environment.GetEnvironmentVariable("GAME_IMAGES_STORAGE");
            string containerName = CONTAINER_NAME;
            string blobName = "afi-welcome-lg.png";

            using var memoryStream = await DownloadBlobAsync(connectionString, containerName, blobName);

            Assert.True(memoryStream.Length > 0, "Blob content should not be empty.");

            ImageConversationService imageService = new ImageConversationService();

            var imageMetadata = imageService.GetImageMetadataFromStreamAsync(memoryStream, blobName);


        }

        private async Task<MemoryStream> DownloadBlobAsync(string? connectionString, string containerName, string blobName)
        {
            var blobClient = new BlobClient(connectionString, containerName, blobName);

            var memoryStream = new MemoryStream();
            await blobClient.DownloadToAsync(memoryStream);
            memoryStream.Position = 0; // Reset stream position after download
            return memoryStream;
        }
    }
}
