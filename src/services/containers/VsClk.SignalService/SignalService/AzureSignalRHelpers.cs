using System.Linq;
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

        private static bool HasAzureSignalRConnections(IConfiguration configuration, string defaultKey, string keyPrefix)
        {
            return configuration.AsEnumerable().Any(pair =>
            {
                var key = pair.Key;
                return ((key == defaultKey && !string.IsNullOrEmpty(pair.Value)) ||
                    (key.StartsWith(keyPrefix) && !string.IsNullOrEmpty(pair.Value)));
            });
        }
    }
}
