

namespace Mcp.ImageOptimizer.Azure.Services;

public interface IAzureResourceService
{
    TokenCredential GetCredential();

    BlobServiceClient GetBlobServiceClient(string storageAccount);
    Task<StorageAccountResource?> GetStorageAccountResourceAsync(string? storageAccount = null, string? subscriptionId = null, CancellationToken cancellationToken = default);
    Task<SubscriptionResource?> GetSubscriptionResourceAsync(ArmClient? armClient = null, string? subscriptionId = null, CancellationToken cancellationToken = default);
}