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
        /// <param name="cloudEnvironmentManager"><see cref="ICloudEnvironmentManager"/>.</param>
        public ShutdownJobHandler(IEnvironmentManager cloudEnvironmentManager)
        {
            this.environmentManager = cloudEnvironmentManager;
        }

        /// <inheritdoc/>
        public bool CanProcess(CollectedData data)
        {
            var jobResult = data as JobResult;
            return string.Equals(jobResult?.Name, JobName, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc/>
        public async Task<CollectedDataHandlerContext> ProcessAsync(CollectedData data, CollectedDataHandlerContext handlerContext, Guid vmResourceId, IDiagnosticsLogger logger)
        {
            if (!CanProcess(data))
            {
                throw new ArgumentException($"Cannot process {data.Name} by {nameof(ShutdownJobHandler)}");
            }

            return await logger.OperationScopeAsync(
                "shutdown_job_handler_process",
                async (childLogger) =>
               {
                   var jobResult = (JobResult)data;

                   childLogger.FluentAddBaseValue(nameof(CollectedData), JsonConvert.SerializeObject(jobResult))
                        .FluentAddBaseValue("CloudEnvironmentId", jobResult.EnvironmentId);

                   var cloudEnvironment = handlerContext.CloudEnvironment;
                   if (cloudEnvironment == null)
                   {
                       return handlerContext;
                   }

                   var environmentServiceResult = await this.environmentManager.SuspendCallbackAsync(cloudEnvironment, logger);
                   return new CollectedDataHandlerContext(environmentServiceResult.CloudEnvironment);
               });
        }
    }
}
