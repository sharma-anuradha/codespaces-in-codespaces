// <copyright file="AzureRedisRelayProviderFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Implements a IAzureRedisProviderServiceFactory for our relay redis provider
    /// </summary>
    public class AzureRedisRelayProviderFactory : IAzureRedisProviderServiceFactory
    {
        private readonly ILogger<AzureRedisRelayProvider> logger;
        private readonly IRelayBackplaneManager backplaneManager;

        public AzureRedisRelayProviderFactory(
            ILogger<AzureRedisRelayProvider> logger,
            IRelayBackplaneManager backplaneManager)
        {
            this.logger = logger;
            this.backplaneManager = backplaneManager;
        }

        public async Task CreateAsync((string ServiceId, string Stamp, string ServiceType) serviceInfo, RedisConnectionPool redisConnectionPool, CancellationToken cancellationToken)
        {
            var backplaneProvider = await AzureRedisRelayProvider.CreateAsync(
                serviceInfo,
                redisConnectionPool,
                this.logger);
            this.backplaneManager.RegisterProvider(backplaneProvider);
        }
    }
}
