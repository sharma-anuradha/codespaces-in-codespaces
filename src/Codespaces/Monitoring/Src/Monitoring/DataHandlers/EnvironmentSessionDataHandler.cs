// <copyright file="EnvironmentSessionDataHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Monitoring.DataHandlers
{
    /// <summary>
    /// Handler for <see cref="EnvironmentSessionData" />.
    /// </summary>
    public class EnvironmentSessionDataHandler : IDataHandler
    {
        /// <inheritdoc />
        public bool CanProcess(CollectedData data)
        {
            return data is EnvironmentSessionData;
        }

        /// <inheritdoc />
        public Task<CollectedDataHandlerContext> ProcessAsync(CollectedData data, CollectedDataHandlerContext handlerContext, Guid vmResourceId, IDiagnosticsLogger logger)
        {
            if (!CanProcess(data))
            {
                throw new InvalidOperationException($"Collected data of type {data?.GetType().Name} cannot be processed by {nameof(EnvironmentSessionDataHandler)}.");
            }

            return logger.OperationScopeAsync(
               "environment_session_data_handler_process",
               (childLogger) =>
               {
                   return Task.FromResult(handlerContext);
               });
        }
    }
}
