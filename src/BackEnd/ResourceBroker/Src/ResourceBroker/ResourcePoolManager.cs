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
        private const int MaxTries = 3;

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
        public Task<ResourceRecord> TryGetAsync(string poolCode, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_try_get",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue("PoolLookupRunId", Guid.NewGuid())
                        .FluentAddBaseValue(ResourceLoggingPropertyConstants.PoolImageFamilyName, poolCode);

                    var trys = 0;
                    var tryAgain = false;
                    var item = (ResourceRecord)null;

                    // Iterate around if we need to
                    while (item == null && trys < MaxTries)
                    {
                        // Conduct core operation
                        (item, tryAgain) = await TryGetAttemptAsync(poolCode, trys, childLogger);

                        // Break out if we don't need to try agian
                        if (!tryAgain)
                        {
                            break;
                        }

                        trys++;
                    }

                    return item;
                });
        }

        /// <inheritdoc/>
        public Task ReleaseGetAsync(string resourceId, IDiagnosticsLogger logger)
        {
            return logger.RetryOperationScopeAsync(
                $"{LogBaseName}_release_get",
                async (childLogger) =>
                {
                    // Fetch record
                    var item = await ResourceRepository.GetAsync(resourceId, childLogger.NewChildLogger());

                    // Update core properties to indicate that its unassigned
                    item.IsAssigned = false;
                    item.Assigned = null;

                    // Update core resource record
                    await ResourceRepository.UpdateAsync(item, childLogger.NewChildLogger());
                });
        }

        private Task<(ResourceRecord, bool)> TryGetAttemptAsync(string poolCode, int trys, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run_try",
                async (childLogger) =>
                {
                    var tryAgain = false;

                    childLogger.FluentAddBaseValue("PoolLookupAttemptRunId", Guid.NewGuid())
                        .FluentAddValue("PoolLookupAttemptTry", trys);

                    // Get core resource record
                    var item = await ResourceRepository.GetPoolReadyUnassignedAsync(poolCode, childLogger.NewChildLogger());

                    childLogger.FluentAddValue("PoolLookupFoundItem", item != null);

                    // Break out if nothing is found
                    if (item != null)
                    {
                        try
                        {
                            // Update core properties to indicate that its assigned
                            item.IsAssigned = true;
                            item.Assigned = DateTime.UtcNow;

                            // Update core resource record
                            await ResourceRepository.UpdateAsync(item, childLogger.NewChildLogger());

                            childLogger.FluentAddValue("PoolLookupUpdateConflict", false);
                        }
                        catch (DocumentClientException ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed)
                        {
                            childLogger.FluentAddValue("PoolLookupUpdateConflict", true);

                            item = null;
                            tryAgain = true;
                        }
                    }

                    return (item, tryAgain);
                });
        }
    }
}
