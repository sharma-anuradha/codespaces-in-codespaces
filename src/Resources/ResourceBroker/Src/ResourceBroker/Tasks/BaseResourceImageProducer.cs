// <copyright file="BaseResourceImageProducer.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// A class that implements IJobSchedulePayloadFactory and able to produce job payloads
    /// for resource images.
    /// </summary>
    public abstract class BaseResourceImageProducer : IJobSchedulePayloadFactory, IJobSchedulerRegister
    {
        public const string FeatureFlagName = "resource-image-producer";

        protected BaseResourceImageProducer(IJobSchedulerFeatureFlags jobSchedulerFeatureFlags)
        {
            JobSchedulerFeatureFlags = jobSchedulerFeatureFlags;
        }

        protected abstract string JobName { get; }

        protected abstract Type JobHandlerType { get; }

        protected abstract Task<IEnumerable<ShareConnectionInfo>> GetStorageAccountsAsync();

        private IJobSchedulerFeatureFlags JobSchedulerFeatureFlags { get; }

        /// <inheritdoc/>
        public void RegisterScheduleJob()
        {
            // Note: run every hour with a 1 day obtained lease.
            // We may change this after we fully deprecate the ported task.
            JobSchedulerFeatureFlags.AddRecurringJobPayload(
                "0 * * * *",
                jobName: JobName,
                ResourceJobQueueConstants.GenericQueueName,
                claimSpan: TimeSpan.FromDays(1),
                this,
                FeatureFlagName);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<(JobPayload, JobPayloadOptions)>> CreatePayloadsAsync(string jobRunId, DateTime scheduleRun, IServiceProvider serviceProvider, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            // Fetch accounts with account name and key
            var accounts = await GetStorageAccountsAsync();

            // return produced payloads
            return accounts.Select(account =>
            {
                var jobPayload = (StorageAccountPayloadBase)Activator.CreateInstance(typeof(StorageAccountPayload<>).MakeGenericType(JobHandlerType));
                jobPayload.StorageAccount = account;
                return ((JobPayload)jobPayload, (JobPayloadOptions)null);
            });
        }

        /// <summary>
        /// A resource pool payload.
        /// </summary>
        /// <typeparam name="T">Type of the job handler.</typeparam>
        public class StorageAccountPayload<T> : StorageAccountPayloadBase
            where T : class
        {
        }

        /// <summary>
        /// Base class for a storage account payload.
        /// </summary>
        public class StorageAccountPayloadBase : JobPayload
        {
            public ShareConnectionInfo StorageAccount { get; set; }
        }
    }
}
