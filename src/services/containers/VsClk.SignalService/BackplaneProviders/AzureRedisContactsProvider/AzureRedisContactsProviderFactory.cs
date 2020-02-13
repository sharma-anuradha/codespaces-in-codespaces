// <copyright file="AzureRedisContactsProviderFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Implements a IAzureRedisProviderServiceFactory for our contacts redis provider
    /// </summary>
    public class AzureRedisContactsProviderFactory : IAzureRedisProviderServiceFactory
    {
        private readonly ILogger<AzureRedisContactsProvider> logger;
        private readonly IContactBackplaneManager backplaneManager;
        private readonly IDataFormatProvider formatProvider;

        public AzureRedisContactsProviderFactory(
            ILogger<AzureRedisContactsProvider> logger,
            IContactBackplaneManager backplaneManager,
            IDataFormatProvider formatProvider = null)
        {
            this.logger = logger;
            this.backplaneManager = backplaneManager;
            this.formatProvider = formatProvider;
        }

        public async Task CreateAsync((string ServiceId, string Stamp) serviceInfo, RedisConnectionPool redisConnectionPool, CancellationToken cancellationToken)
        {
            var backplaneProvider = await AzureRedisContactsProvider.CreateAsync(
                serviceInfo,
                redisConnectionPool,
                this.logger,
                this.formatProvider);

            // Note: the redis provider does not support an optimized 'GetContacts' capability
            // so when used with another provider with better support it will be discarded
            var supportsLevel = new ContactBackplaneProviderSupportLevel()
            {
                GetContacts = BackplaneProviderSupportLevelConst.MinimumSupportThreshold,
            };
            this.backplaneManager.RegisterProvider(
                backplaneProvider,
                supportsLevel);
        }
    }
}
