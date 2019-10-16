// <copyright file="ShutdownJobHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Monitoring.DataHandlers
{
    /// <summary>
    /// Handler for processing output of VSO Agent jobs from the virtual machine.
    /// </summary>
    public class ShutdownJobHandler : IDataHandler
    {
        private const string JobName = "ShutdownEnvironment";
        private ICloudEnvironmentManager cloudEnvironmentManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShutdownJobHandler"/> class.
        /// </summary>
        /// <param name="cloudEnvironmentManager"><see cref="ICloudEnvironmentManager"/>.</param>
        public ShutdownJobHandler(ICloudEnvironmentManager cloudEnvironmentManager)
        {
            this.cloudEnvironmentManager = cloudEnvironmentManager;
        }

        /// <inheritdoc/>
        public bool CanProcess(CollectedData data)
        {
            var jobResult = data as JobResult;
            return string.Equals(jobResult?.Name, JobName, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc/>
        public async Task ProcessAsync(CollectedData data, Guid vmResourceId, IDiagnosticsLogger logger)
        {
            if (!CanProcess(data))
            {
                throw new ArgumentException($"Cannot process {data.Name} by {nameof(ShutdownJobHandler)}");
            }

            var jobResult = (JobResult)data;
            await this.cloudEnvironmentManager.ShutdownEnvironmentCallbackAsync(jobResult.Id, jobResult.JobState == JobState.Succeeded, logger);
        }
    }
}
