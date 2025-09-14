using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Storage;

namespace Mcp.ImageOptimizer.Azure.Tools;

public interface IAzureResourceService
{
    TokenCredential GetCredential();
    Task<StorageAccountResource?> GetStorageAccountResourceAsync(string storageAccount, string? subscriptionId = null, CancellationToken cancellationToken = default);
    Task<SubscriptionResource?> GetSubscriptionResourceAsync(ArmClient armClient, string? subscriptionId = null, CancellationToken cancellation = default);
}