// <copyright file="AzureDocumentsProviderFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Factory class for our AzureDocumentsProvider type.
    /// </summary>
    public class AzureDocumentsProviderFactory : IAzureDocumentsProviderServiceFactory
    {
        private readonly IContactBackplaneManager backplaneManager;
        private readonly ILogger<AzureDocumentsProvider> logger;
        private readonly IServiceCounters serviceCounters;
        private readonly IDataFormatProvider formatProvider;

        public AzureDocumentsProviderFactory(
            IContactBackplaneManager backplaneManager,
            ILogger<AzureDocumentsProvider> logger,
            IServiceCounters serviceCounters = null,
            IDataFormatProvider formatProvider = null)
        {
            this.backplaneManager = backplaneManager;
            this.logger = logger;
            this.serviceCounters = serviceCounters;
            this.formatProvider = formatProvider;
        }

        /// <inheritdoc/>
        public async Task CreateAsync(ServiceInfo serviceInfo, DatabaseSettings databaseSettings, CancellationToken cancellationToken)
        {
            var backplaneProvider = await AzureDocumentsProvider.CreateAsync(
                serviceInfo,
                databaseSettings,
                this.logger,
                this.serviceCounters,
                this.formatProvider);
            this.backplaneManager.RegisterProvider(backplaneProvider);
        }
    }
}
