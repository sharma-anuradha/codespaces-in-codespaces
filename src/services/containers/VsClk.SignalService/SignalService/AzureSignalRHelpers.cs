using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.SignalR;
using Microsoft.Extensions.Configuration;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Azure SignalR helpers
    /// </summary>
    internal static class AzureSignalRHelpers
    {
        public const string ConnectionStringDefaultKey = "Azure:SignalR:ConnectionString";

        public static readonly string ConnectionStringSecondaryKey =
            $"ConnectionStrings:{ConnectionStringDefaultKey}";

        public static readonly string ConnectionStringKeyPrefix = $"{ConnectionStringDefaultKey}:";

        public static readonly string ConnectionStringSecondaryKeyPrefix = $"{ConnectionStringSecondaryKey}:";

        /// <summary>
        /// Look for Azure connections string on our app configuration
        /// </summary>
        /// <param name="configuration">The app configuration</param>
        /// <returns></returns>
        public static bool HasAzureSignalRConnections(this IConfiguration configuration)
        {
            return
                HasAzureSignalRConnections(configuration, ConnectionStringDefaultKey, ConnectionStringKeyPrefix) ||
                HasAzureSignalRConnections(configuration, ConnectionStringSecondaryKey, ConnectionStringSecondaryKeyPrefix);
        }

        /// <summary>
        /// Return all Azure SignalR connections that were configured
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IEnumerable<KeyValuePair<string, string>> GetAllAzureSignalRConnections(IConfiguration configuration)
        {
            return
                GetAzureSignalRConnections(configuration, ConnectionStringDefaultKey, ConnectionStringKeyPrefix).Union
                (GetAzureSignalRConnections(configuration, ConnectionStringSecondaryKey, ConnectionStringSecondaryKeyPrefix)).Distinct();
        }

        public static ServiceEndpoint[] GetAzureSignalRServiceEndpoints(this IConfiguration configuration)
        {
            return GetAllAzureSignalRConnections(configuration).Select(kvp =>
            {
                return new ServiceEndpoint(
                    kvp.Value,
                    kvp.Key == ConnectionStringDefaultKey ? EndpointType.Primary : EndpointType.Secondary,
                    kvp.Key);
            }).ToArray();
        }

        public static async Task<ServiceEndpoint[]> GetAzureSignalRServiceEndpointsAsync(
            this ApplicationServicePrincipal applicationServicePrincipal,
            string keyVaultName,
            string stamp)
        {
            var matchName = $"Config-SignalRConnectionString-{stamp}";

            return (await applicationServicePrincipal.GetSecretItemsAsync(
                keyVaultName,
                name => name.StartsWith(matchName, StringComparison.InvariantCultureIgnoreCase))).
                Select(kvp => new ServiceEndpoint(
                                kvp.Value,
                                kvp.Key.Length == matchName.Length || kvp.Key.Contains("Primary", StringComparison.InvariantCultureIgnoreCase) ? EndpointType.Primary : EndpointType.Secondary,
                                kvp.Key)).ToArray();
        }

        private static IEnumerable<KeyValuePair<string,string>> GetAzureSignalRConnections(IConfiguration configuration, string defaultKey, string keyPrefix)
        {
            return configuration.AsEnumerable().Where(pair =>
            {
                var key = pair.Key;
                return ((key == defaultKey && !string.IsNullOrEmpty(pair.Value)) ||
                    (key.StartsWith(keyPrefix) && !string.IsNullOrEmpty(pair.Value)));
            });
        }

        private static bool HasAzureSignalRConnections(IConfiguration configuration, string defaultKey, string keyPrefix)
        {
            return GetAzureSignalRConnections(configuration, defaultKey, keyPrefix).Any();
        }
    }
}
