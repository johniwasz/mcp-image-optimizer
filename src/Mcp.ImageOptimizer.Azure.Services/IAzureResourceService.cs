using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Storage;
using Azure.Storage.Blobs;

namespace Mcp.ImageOptimizer.Azure.Services;

public interface IAzureResourceService
{
    TokenCredential GetCredential();

    BlobServiceClient GetBlobServiceClient(string storageAccount);
    Task<StorageAccountResource?> GetStorageAccountResourceAsync(string storageAccount, string? subscriptionId = null, CancellationToken cancellationToken = default);
    Task<SubscriptionResource?> GetSubscriptionResourceAsync(ArmClient? armClient = null, string? subscriptionId = null, CancellationToken cancellationToken = default);
}