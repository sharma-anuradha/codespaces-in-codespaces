// <copyright file="ShutdownJobHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Monitoring.DataHandlers
{
    /// <summary>
    /// Handler for processing output of VSO Agent jobs from the virtual machine.
    /// </summary>
    public class ShutdownJobHandler : IDataHandler
    {
        private const string JobName = "ShutdownEnvironment";
        private IEnvironmentManager environmentManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShutdownJobHandler"/> class.
        /// </summary>
        /// <param name="environmentManager"><see cref="IEnvironmentManager"/>.</param>
        public ShutdownJobHandler(IEnvironmentManager environmentManager)
        {
            this.environmentManager = environmentManager;
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

            await logger.OperationScopeAsync(
                "shutdown_job_handler_process",
                async (childLogger) =>
               {
                   var jobResult = (JobResult)data;

                   childLogger.FluentAddBaseValue(nameof(CollectedData), JsonConvert.SerializeObject(jobResult))
                        .FluentAddBaseValue("CloudEnvironmentId", jobResult.EnvironmentId);

                   var environment = await this.environmentManager.GetAsync(jobResult.Id, logger);
                   if (environment == null)
                   {
                       return;
                   }

                   await this.environmentManager.SuspendCallbackAsync(environment, logger);
               });
        }
    }
}
