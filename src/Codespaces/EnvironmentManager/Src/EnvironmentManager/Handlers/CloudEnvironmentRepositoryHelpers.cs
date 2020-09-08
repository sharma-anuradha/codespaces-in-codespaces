// <copyright file="CloudEnvironmentRepositoryHelpers.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers
{
    /// <summary>
    /// Helpers for the ICloudEnvironmentRepository interface.
    /// </summary>
    internal static class CloudEnvironmentRepositoryHelpers
    {
        /// <summary>
        /// Update the record of an env record.
        /// </summary>
        /// <param name="cloudEnvironmentRepository">Repo instance.</param>
        /// <param name="environmentId">Id.</param>
        /// <param name="record">Env record.</param>
        /// <param name="mutateRecordCallback">Callback to mutate the record.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="operationBaseName">Operation name.</param>
        /// <returns>Completion task.</returns>
        public static async Task<bool> UpdateRecordAsync(
            this ICloudEnvironmentRepository cloudEnvironmentRepository,
            Guid environmentId,
            EnvironmentRecordRef record,
            Func<CloudEnvironment, IDiagnosticsLogger, Task<bool>> mutateRecordCallback,
            IDiagnosticsLogger logger,
            string operationBaseName)
        {
            var stateChanged = false;

            // retry till we succeed
            await logger.RetryOperationScopeAsync(
                $"{operationBaseName}_status_update",
                async (innerLogger) =>
                {
                    // Obtain a fresh record.
                    record.Value = (await FetchReferenceAsync(cloudEnvironmentRepository, environmentId, innerLogger)).Value;

                    // Mutate record
                    stateChanged = await mutateRecordCallback(record.Value, innerLogger);

                    // Only need to update things if something has changed
                    if (stateChanged)
                    {
                        record.Value = await cloudEnvironmentRepository.UpdateAsync(record.Value, innerLogger.NewChildLogger());
                    }
                });

            return stateChanged;
        }

        /// <summary>
        /// Fetch a record from the env db record.
        /// </summary>
        /// <param name="cloudEnvironmentRepository">Repo instance.</param>
        /// <param name="resourceId">Id.</param>
        /// <param name="logger">Logger instance.</param>
        /// <returns>Completion task with env record isntance.</returns>
        public static async Task<EnvironmentRecordRef> FetchReferenceAsync(
            this ICloudEnvironmentRepository cloudEnvironmentRepository,
            Guid resourceId,
            IDiagnosticsLogger logger)
        {
            // Pull record
            var resource = await cloudEnvironmentRepository.GetAsync(resourceId.ToString(), logger.NewChildLogger());
            if (resource == null)
            {
                logger.FluentAddValue("HandlerFailedToFindResource", true);

                throw new CloudEnvironmentNotFoundException(resourceId);
            }

            return new EnvironmentRecordRef(resource);
        }
    }
}
