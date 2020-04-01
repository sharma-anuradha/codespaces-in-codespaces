// <copyright file="CrossRegionStorageQueueCollection.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Queue;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Queue
{
    /// <summary>
    /// Provides configuration and policy for a Queue DB collection. Ensures that
    /// the configured queue and collection exist.
    /// This allows sending messges cross region.
    /// </summary>
    public abstract class CrossRegionStorageQueueCollection : CrossRegionStorageQueueCollectionBase, ICrossRegionStorageQueueCollection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CrossRegionStorageQueueCollection"/> class.
        /// </summary>
        /// <param name="clientProvider">The client provider.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="controlPlaneInfo">Control plane info.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="resourceNameBuilder">The resource name builder.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public CrossRegionStorageQueueCollection(
            ICrossRegionStorageQueueClientProvider clientProvider,
            IHealthProvider healthProvider,
            IControlPlaneInfo controlPlaneInfo,
            IDiagnosticsLoggerFactory loggerFactory,
            IResourceNameBuilder resourceNameBuilder,
            LogValueSet defaultLogValues)
            : base(clientProvider, healthProvider, controlPlaneInfo, loggerFactory, resourceNameBuilder, defaultLogValues)
        {
        }

        /// <summary>
        /// Gets the name that should be logging.
        /// </summary>
        protected abstract string LoggingDocumentName { get; }

        /// <summary>Use this only for cross region message passing.</summary>
        /// <inheritdoc/>
        public async Task AddAsync(string content, AzureLocation controlPlaneRegion, TimeSpan? initialVisibilityDelay, IDiagnosticsLogger logger)
        {
            await logger.OperationScopeAsync(
               $"azurequeue_{LoggingDocumentName}_create",
               async (childLogger) =>
               {
                   var queue = await GetQueueAsync(controlPlaneRegion);
                   var message = new CloudQueueMessage(content);

                   childLogger.FluentAddValue("QueueVisibilityDelay", initialVisibilityDelay);

                   await queue.AddMessageAsync(message, null, initialVisibilityDelay, null, null);
               });
        }
    }
}
