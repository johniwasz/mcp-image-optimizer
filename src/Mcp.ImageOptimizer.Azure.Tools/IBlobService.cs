using Azure.Storage.Blobs;
using Mcp.ImageOptimizer.Azure.Tools.Models;
using Mcp.ImageOptimizer.Common;

namespace Mcp.ImageOptimizer.Azure.Tools
{
    public interface IBlobService
    {
        Task<MemoryStream> DownloadBlobAsync(string accountName, string containerName, string blobName);
        Task<ImageMetadata> GetImageMetadataAsync(string storageAccountName, string name1, string name2);
        Task<IEnumerable<StorageAccountInfo>> ListStorageAccountsAsync(string? region = null, string? subscriptionId = null);
        Task UploadBlobAsync(BlobContainerClient blobContainer, string newBlobName, MemoryStream newBlob);

        Task<IEnumerable<ConvertedImageMetadata>> ConvertImageAndGetMetadataAsync(string storageAccountName, int quality, bool deleteOriginal);
    }
}