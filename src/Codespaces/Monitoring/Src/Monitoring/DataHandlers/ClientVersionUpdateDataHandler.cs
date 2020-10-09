// <copyright file="ClientVersionUpdateDataHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Monitoring.DataHandlers
{
    /// <summary>
    /// Handler for <see cref="ClientVersionUpdateData" />.
    /// </summary>
    public class ClientVersionUpdateDataHandler : IDataHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClientVersionUpdateDataHandler"/> class.
        /// </summary>
        /// <param name="environmentManager">Environment Manager.</param>
        public ClientVersionUpdateDataHandler(
            IEnvironmentManager environmentManager)
        {
            EnvironmentManager = Requires.NotNull(environmentManager, nameof(environmentManager));
        }

        private IEnvironmentManager EnvironmentManager { get; }

        /// <inheritdoc />
        public bool CanProcess(CollectedData data)
        {
            return data is ClientVersionUpdateData;
        }

        /// <inheritdoc />
        public async Task<CollectedDataHandlerContext> ProcessAsync(CollectedData data, CollectedDataHandlerContext handlerContext, Guid vmResourceId, IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
               "client_version_update_data_handler_process",
               async (childLogger) =>
               {
                   if (!CanProcess(data))
                   {
                       throw new InvalidOperationException($"Collected data of type {data?.GetType().Name} cannot be processed by {nameof(ClientVersionUpdateDataHandler)}.");
                   }

                   var clientVersionData = (ClientVersionUpdateData)data;
                   var environmentTransition = handlerContext.CloudEnvironmentTransition;

                   childLogger.FluentAddBaseValue(nameof(CollectedData), JsonConvert.SerializeObject(clientVersionData))
                       .FluentAddBaseValue("ComputeResourceId", vmResourceId)
                       .AddBaseEnvironmentId(Guid.Parse(clientVersionData.EnvironmentId))
                       .FluentAddValue("CloudEnvironmentFound", environmentTransition != null)
                       .FluentAddBaseValue("JobCollectedData", JsonConvert.SerializeObject(clientVersionData))
                       .FluentAddBaseValue("JobState", clientVersionData.UpdateState);

                   // No-op if the environmentId is empty.
                   if (string.IsNullOrWhiteSpace(clientVersionData.EnvironmentId))
                   {
                       return handlerContext;
                   }

                   if (environmentTransition == null)
                   {
                       return handlerContext;
                   }

                   ValidateCloudEnvironment(environmentTransition.Value, clientVersionData.EnvironmentId);

                   if (environmentTransition.Value.SystemStatusInfo?.UpdateState != clientVersionData.UpdateState ||
                       environmentTransition.Value.SystemStatusInfo?.VsVersion != clientVersionData.VsVersion)
                   {
                       environmentTransition.PushTransition(
                           (env) =>
                           {
                               if (env.SystemStatusInfo == null)
                               {
                                   env.SystemStatusInfo = new SystemStatusInfo
                                   {
                                       UpdateState = clientVersionData.UpdateState,
                                       VsVersion = clientVersionData.VsVersion,
                                   };
                               }
                               else
                               {
                                   env.SystemStatusInfo.UpdateState = clientVersionData.UpdateState;
                                   env.SystemStatusInfo.VsVersion = clientVersionData.VsVersion;
                               }
                           });
                   }

                   if (clientVersionData.UpdateState == JobState.Succeeded ||
                       clientVersionData.UpdateState == JobState.Failed)
                   {
                       // If updating, shutdown after we're done
                       if (environmentTransition.Value.State == CloudEnvironmentState.Updating)
                       {
                           // If update is done, shutdown environment
                           await EnvironmentManager.ForceSuspendAsync(Guid.Parse(environmentTransition.Value.Id), logger.NewChildLogger());

                           // Reset environment record and transition
                           environmentTransition.ReplaceAndResetTransition(default);
                       }
                   }

                   return handlerContext;
               });
        }

        private void ValidateCloudEnvironment(CloudEnvironment cloudEnvironment, string inputEnvironmentId)
        {
            ValidationUtil.IsTrue(cloudEnvironment != null, $"No environments found matching the EnvironmentId {inputEnvironmentId} from {nameof(EnvironmentSessionData)}");
            ValidationUtil.IsTrue(cloudEnvironment.State != CloudEnvironmentState.Deleted, $"Heartbeat received for a deleted environment {inputEnvironmentId}");
        }
    }
}