using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Storage;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mcp.ImageOptimizer.Azure.Tools;

public class AzureResourceService : IAzureResourceService
{

    public TokenCredential GetCredential()
    {
        // 1) Prefer environment variables (standard names used by Azure SDKs)
        var envTenant = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
        var envClient = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
        var envSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");

        if (!string.IsNullOrWhiteSpace(envTenant) &&
            !string.IsNullOrWhiteSpace(envClient) &&
            !string.IsNullOrWhiteSpace(envSecret))
        {
            // DefaultAzureCredential will pick up environment credentials as EnvironmentCredential,
            // so we can return DefaultAzureCredential here too. But to be explicit:
            return new ClientSecretCredential(envTenant!, envClient!, envSecret!);
        }

        // 2) Fallback to app.config appSettings (requires System.Configuration.ConfigurationManager nuget)
        var cfgTenant = ConfigurationManager.AppSettings["AZURE_TENANT_ID"];
        var cfgClient = ConfigurationManager.AppSettings["AZURE_CLIENT_ID"];
        var cfgSecret = ConfigurationManager.AppSettings["AZURE_CLIENT_SECRET"];

        if (!string.IsNullOrWhiteSpace(cfgTenant) &&
            !string.IsNullOrWhiteSpace(cfgClient) &&
            !string.IsNullOrWhiteSpace(cfgSecret))
        {
            return new ClientSecretCredential(cfgTenant!, cfgClient!, cfgSecret!);
        }

        // 3) Final fallback: DefaultAzureCredential (checks Azure CLI, VS sign-in, Managed Identity, etc.)
        return new DefaultAzureCredential();
    }

    public async Task<SubscriptionResource?> GetSubscriptionResourceAsync(ArmClient armClient, string? subscriptionId = null, CancellationToken cancellationToken = default)
    {
        SubscriptionResource? retResource = null;

        try
        {
            if (!string.IsNullOrWhiteSpace(subscriptionId))
            {
                var subId = subscriptionId.Trim();
                var subResourceId = new ResourceIdentifier($"/subscriptions/{subId}");
                retResource = armClient.GetSubscriptionResource(subResourceId);
                // Ensure the resource exists by calling GetAsync (will throw if not found / not authorized)
                await retResource.GetAsync(cancellationToken);
            }
            else
            {
                // Try to get the default subscription. Fall back to the first available subscription.
                try
                {
                    retResource = await armClient.GetDefaultSubscriptionAsync(cancellationToken);
                }
                catch (RequestFailedException)
                {
                    await foreach (var sub in armClient.GetSubscriptions().GetAllAsync(cancellationToken))
                    {
                        retResource = sub;
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Unable to resolve Azure subscription. Ensure you are authenticated and have access to a subscription.", ex);
        }

        return retResource;
    }

    public async Task<StorageAccountResource?> GetStorageAccountResourceAsync(string storageAccount, string? subscriptionId = null, CancellationToken cancellationToken = default)
    {
        StorageAccountResource? retResource = null;

        SubscriptionResource? subscriptionResource = await GetSubscriptionResourceAsync(new ArmClient(GetCredential()), subscriptionId);

        // List all storage accounts in the subscription
        var storageAccounts = subscriptionResource.GetStorageAccountsAsync(cancellationToken);

        await foreach (StorageAccountResource account in storageAccounts)
        {
            if (account.Data.Name.Equals(storageAccount, StringComparison.OrdinalIgnoreCase))
            {
                retResource = account;
                break;
            }
        }
        return retResource;
    }
}
