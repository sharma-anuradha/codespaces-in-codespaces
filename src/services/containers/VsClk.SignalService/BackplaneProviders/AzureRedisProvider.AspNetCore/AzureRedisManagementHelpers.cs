using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Redis.Fluent;
using Microsoft.Extensions.Logging;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Helpers for the Azure Management Fluent fro Redis
    /// </summary>
    internal static class AzureRedisManagementHelpers
    {
        public static bool IsAzureRedisConnectionDefined(
    this ApplicationServicePrincipal applicationServicePrincipal,
    AppSettingsBase appSettings)
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
            AppSettingsBase appSettings,
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
    }
}
