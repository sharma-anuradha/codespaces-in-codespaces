using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.CosmosDB.Fluent;
using Microsoft.Azure.Management.Redis.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Extensions.Logging;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Helpers for the Azure Management Fluent
    /// </summary>
    internal static class AzureManagementHelpers
    {
        public static bool IsDefined(
            this ApplicationServicePrincipal applicationServicePrincipal)
        {
            return applicationServicePrincipal != null &&
                !
                (string.IsNullOrEmpty(applicationServicePrincipal.ClientId) ||
                string.IsNullOrEmpty(applicationServicePrincipal.ClientPassword) ||
                string.IsNullOrEmpty(applicationServicePrincipal.TenantId));
        }

        public static bool IsAzureCosmosConnectionDefined(
            this ApplicationServicePrincipal applicationServicePrincipal,
            AppSettings appSettings)
        {
            Requires.NotNull(appSettings, nameof(appSettings));

            return applicationServicePrincipal.IsDefined() &&
                !string.IsNullOrEmpty(appSettings.SubscriptionId) &&
                !string.IsNullOrEmpty(appSettings.ResourceGroupInstanceName) &&
                !string.IsNullOrEmpty(appSettings.AzureCosmosDbName);
        }

        public static async Task<(string, string)> GetAzureCosmosConnection(
            this ApplicationServicePrincipal applicationServicePrincipal,
            ILogger logger,
            AppSettings appSettings,
            CancellationToken cancellationToken)
        {
            Requires.NotNull(applicationServicePrincipal, nameof(applicationServicePrincipal));
            Requires.NotNull(logger, nameof(logger));
            Requires.NotNull(appSettings, nameof(appSettings));

            logger.LogInformation($"Get Azure cosmos database connection name:{appSettings.AzureCosmosDbName} resource group:{appSettings.ResourceGroupInstanceName} subscriptionId:{appSettings.SubscriptionId}");

            var cosmosDBManager = CosmosDBManager.Authenticate(applicationServicePrincipal.GetAzureCredentials(), appSettings.SubscriptionId);
            var cosmosDbAccount = await cosmosDBManager.CosmosDBAccounts.GetByResourceGroupAsync(appSettings.ResourceGroupInstanceName, appSettings.AzureCosmosDbName, cancellationToken);

            if (cosmosDbAccount == null)
            {
                throw new Exception($"Cosmos DB name:{appSettings.AzureCosmosDbName} not found on resource group:{appSettings.ResourceGroupInstanceName} subscriptionId:{appSettings.SubscriptionId}");
            }

            var endpoint = cosmosDbAccount.DocumentEndpoint;
            var keys = await cosmosDbAccount.ListKeysAsync();
            var key = keys.PrimaryMasterKey;
            return (endpoint, key);
        }

        public static bool IsAzureRedisConnectionDefined(
            this ApplicationServicePrincipal applicationServicePrincipal,
            AppSettings appSettings)
        {
            Requires.NotNull(appSettings, nameof(appSettings));

            return applicationServicePrincipal.IsDefined() &&
                !string.IsNullOrEmpty(appSettings.SubscriptionId) &&
                !string.IsNullOrEmpty(appSettings.ResourceGroupInstanceName) &&
                !string.IsNullOrEmpty(appSettings.AzureCacheRedisName);
        }

        public static async Task<string> GetAzureRedisConnection(
            this ApplicationServicePrincipal applicationServicePrincipal,
            ILogger logger,
            AppSettings appSettings,
            CancellationToken cancellationToken)
        {
            Requires.NotNull(applicationServicePrincipal, nameof(applicationServicePrincipal));
            Requires.NotNull(logger, nameof(logger));
            Requires.NotNull(appSettings, nameof(appSettings));

            logger.LogInformation($"Get Azure Cache redis connection name:{appSettings.AzureCacheRedisName} resource group:{appSettings.ResourceGroupInstanceName} subscriptionId:{appSettings.SubscriptionId}");

            var redisManager = RedisManager.Authenticate(applicationServicePrincipal.GetAzureCredentials(), appSettings.SubscriptionId);
            var redisCache = await redisManager.RedisCaches.GetByResourceGroupAsync(appSettings.ResourceGroupInstanceName, appSettings.AzureCacheRedisName, cancellationToken);
            if (redisCache == null)
            {
                throw new Exception($"Redis cache name:{appSettings.AzureCacheRedisName} not found on resource group:{appSettings.ResourceGroupInstanceName} subscriptionId:{appSettings.SubscriptionId}");
            }

            return $"{redisCache.HostName},password={redisCache.GetKeys().PrimaryKey},ssl=True,abortConnect=False";
        }

        private static AzureCredentials GetAzureCredentials(this ApplicationServicePrincipal applicationServicePrincipal)
        {
            return new AzureCredentialsFactory()
                .FromServicePrincipal(
                    applicationServicePrincipal.ClientId,
                    applicationServicePrincipal.ClientPassword,
                    applicationServicePrincipal.TenantId,
                    AzureEnvironment.AzureGlobalCloud);
        }
    }
}
