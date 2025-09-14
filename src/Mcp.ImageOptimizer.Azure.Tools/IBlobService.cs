using Azure.Storage.Blobs;
using Mcp.ImageOptimizer.Azure.Tools.Models;
using Mcp.ImageOptimizer.Common.Models;

namespace Mcp.ImageOptimizer.Azure.Tools;

public interface IBlobService
{
    Task<MemoryStream> DownloadBlobAsync(string accountName, string containerName, string blobName, CancellationToken cancellationToken = default);
    Task<ImageMetadata> GetImageMetadataAsync(string storageAccountName, string containerName, string blobName, CancellationToken cancellationToken = default);
    Task<IEnumerable<StorageAccountInfo>> ListStorageAccountsAsync(string? region = null, string? subscriptionId = null, CancellationToken cancellationToken = default);
    Task UploadBlobAsync(BlobContainerClient blobContainer, string newBlobName, MemoryStream newBlob, CancellationToken cancellationToken = default);

    Task<IEnumerable<ConvertedImageMetadata>> ConvertImageAndGetMetadataAsync(string storageAccountName, int quality, bool deleteOriginal, CancellationToken cancellationToken = default);
}