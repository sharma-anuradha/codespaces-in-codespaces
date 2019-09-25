﻿// <copyright file="ResourcePoolManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
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
    public class ResourcePoolManager : IResourcePoolManager, IResourcePoolSettingsHandler
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
            EnabledState = new Dictionary<string, bool>();
        }

        private IResourceRepository ResourceRepository { get; }

        private IDictionary<string, bool> EnabledState { get; set; }

        /// <inheritdoc/>
        public Task UpdateResourceEnabledStateAsync(IDictionary<string, bool> enabledState)
        {
            EnabledState = enabledState;

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public bool IsPoolEnabled(string poolCode)
        {
            var isPoolEnabledFound = EnabledState.TryGetValue(poolCode, out var isPoolEnabled);

            return isPoolEnabledFound ? isPoolEnabled : true;
        }

        /// <inheritdoc/>
        public Task<ResourceRecord> TryGetAsync(string poolCode, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run",
                async () =>
                {
                    logger.FluentAddBaseValue("PoolLookupRunId", Guid.NewGuid())
                        .FluentAddBaseValue("PoolImageFamilyName", poolCode);

                    var trys = 0;
                    var tryAgain = false;
                    var item = (ResourceRecord)null;

                    // Iterate around if we need to
                    while (item == null && trys < 3)
                    {
                        var childLogger = logger.WithValues(new LogValueSet())
                            .FluentAddBaseValue("PoolLookupTry", trys);

                        // Conduct core operation
                        (item, tryAgain) = await childLogger.OperationScopeAsync(
                            $"{LogBaseName}_run_try",
                            () => TryGetInnerAsync(poolCode, childLogger));

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

        private async Task<(ResourceRecord, bool)> TryGetInnerAsync(string poolCode, IDiagnosticsLogger logger)
        {
            var tryAgain = false;

            logger.FluentAddBaseValue("PoolLookupAttemptRunId", Guid.NewGuid());

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
