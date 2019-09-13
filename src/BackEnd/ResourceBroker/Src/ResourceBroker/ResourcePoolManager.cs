// <copyright file="ResourcePoolManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
{
    /// <summary>
    /// Manages the underlying resource pools.
    /// </summary>
    public class ResourcePoolManager : IResourcePoolManager
    {
        private const string LogBaseName = ResourceLoggingConstants.ResourcePoolManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourcePoolManager"/> class.
        /// </summary>
        /// <param name="resourceRepository">Target resource repository.</param>
        public ResourcePoolManager(
            IResourceRepository resourceRepository)
        {
            ResourceRepository = Requires.NotNull(resourceRepository, nameof(resourceRepository));
        }

        private IResourceRepository ResourceRepository { get; }

        /// <inheritdoc/>
        public async Task<ResourceRecord> TryGetAsync(string poolCode, IDiagnosticsLogger rootLogger)
        {
            // Setup logging
            var duration = rootLogger.StartDuration();

            rootLogger.FluentAddBaseValue("PoolCode", poolCode);

            var trys = 0;
            var tryAgain = false;
            var item = (ResourceRecord)null;

            // Iterate around if we need to
            while (item == null && trys < 3)
            {
                var logger = rootLogger.WithValues(new LogValueSet())
                    .FluentAddBaseValue("PoolLookupTry", trys);

                // Conduct core operation
                (item, tryAgain) = await logger.OperationScopeAsync(LogBaseName, () => TryGetInnerAsync(poolCode, logger));

                // Break out if we don't need to try agian
                if (!tryAgain)
                {
                    break;
                }

                trys++;
            }

            return item;
        }

        private async Task<(ResourceRecord, bool)> TryGetInnerAsync(string poolCode, IDiagnosticsLogger logger)
        {
            var tryAgain = false;

            // Get core resource record
            var item = await ResourceRepository.GetPoolReadyUnassignedAsync(poolCode, logger);

            logger.FluentAddValue("PoolLookupFoundItem", item != null);

            // Break out if nothing is found
            if (item != null)
            {
                try
                {
                    // Update core properties to indicate that its assigned
                    item.IsAssigned = true;
                    item.Assigned = DateTime.UtcNow;

                    // Update core resource record
                    await ResourceRepository.UpdateAsync(item, logger);

                    logger.FluentAddValue("PoolLookupUpdateConflict", false);
                }
                catch (DocumentClientException ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed)
                {
                    logger.FluentAddValue("PoolLookupUpdateConflict", true);

                    item = null;
                    tryAgain = true;
                }
            }

            return (item, tryAgain);
        }
    }
}
