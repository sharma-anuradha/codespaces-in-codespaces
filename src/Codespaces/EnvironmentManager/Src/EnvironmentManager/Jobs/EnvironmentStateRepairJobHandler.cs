// <copyright file="EnvironmentStateRepairJobHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts.Actions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Monitor;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers
{
    public class EnvironmentStateRepairJobHandler : JobHandlerPayloadBase<EnvironmentStateRepairJobProducer.EnvironmentStateRepairPayload>, IJobHandlerTarget
    {
        public const string LogBaseName = "environment_state_repair_job_handler";

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentStateRepairJobHandler"/> class.
        /// <param name="environmentRepository"></param>
        /// <param name="environmentSuspendAction"></param>
        /// <param name="environmentFailAction"></param>
        /// <param name="environmentDeleteAction"></param>
        /// <param name="superuserIdentity"></param>
        /// <param name="currentIdentityProvider"></param>
        /// </summary>
        public EnvironmentStateRepairJobHandler(
            ICloudEnvironmentRepository environmentRepository,
            IEnvironmentSuspendAction environmentSuspendAction,
            IEnvironmentFailAction environmentFailAction,
            IEnvironmentHardDeleteAction environmentDeleteAction,
            VsoSuperuserClaimsIdentity superuserIdentity,
            ICurrentIdentityProvider currentIdentityProvider)
        {
            EnvironmentRepository = Requires.NotNull(environmentRepository, nameof(environmentRepository));
            EnvironmentSuspendAction = Requires.NotNull(environmentSuspendAction, nameof(environmentSuspendAction));
            EnvironmentDeleteAction = Requires.NotNull(environmentDeleteAction, nameof(environmentDeleteAction));
            EnvironmentFailAction = Requires.NotNull(environmentFailAction, nameof(environmentFailAction));
            SuperuserIdentity = Requires.NotNull(superuserIdentity, nameof(superuserIdentity));
            CurrentIdentityProvider = Requires.NotNull(currentIdentityProvider, nameof(currentIdentityProvider));
        }

        public IJobHandler JobHandler => this;

        public string QueueId => EnvironmentJobQueueConstants.EnvironmentStateRepairJob;

        public AzureLocation? Location => null;

        private ICloudEnvironmentRepository EnvironmentRepository { get; }

        private IEnvironmentSuspendAction EnvironmentSuspendAction { get; }

        private IEnvironmentHardDeleteAction EnvironmentDeleteAction { get; }

        private IEnvironmentFailAction EnvironmentFailAction { get; }

        private VsoSuperuserClaimsIdentity SuperuserIdentity { get; }

        private ICurrentIdentityProvider CurrentIdentityProvider { get; }

        protected override async Task HandleJobAsync(EnvironmentStateRepairJobProducer.EnvironmentStateRepairPayload payload, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            await logger.OperationScopeAsync(
               $"{LogBaseName}_run",
               async (childLogger) =>
               {
                   var environment = await EnvironmentRepository.GetAsync(payload.EnvironmentId, childLogger.NewChildLogger());

                   if (environment == null)
                   {
                       childLogger.FluentAddValue("ErrorReason", "EnvironmentRecordNotFound");
                       return;
                   }

                   childLogger.AddEnvironmentId(environment.Id)
                        .FluentAddBaseValue("EnvironmentCurrentState", environment.State);

                   if (environment.State == payload.CurrentState)
                   {
                       // the cleanup actions require the super user identity
                       using (CurrentIdentityProvider.SetScopedIdentity(SuperuserIdentity))
                       {
                           // the environment is still in the wrong state, need clean up
                           await HandleCleanUpAsync(environment, childLogger.NewChildLogger());
                           childLogger.FluentAddBaseValue("EnvironmentStateAfterRepair", environment.State);
                       }
                   }
               },
               swallowException: true);
        }

        private async Task HandleCleanUpAsync(CloudEnvironment environment, IDiagnosticsLogger logger)
        {
            if (environment.Type == EnvironmentType.StaticEnvironment)
            {
                // no clean up is performed for static environments
                return;
            }

            switch (environment.State)
            {
                case CloudEnvironmentState.Unavailable:
                    // Unavailable -> Suspend
                    await EnvironmentSuspendAction.RunAsync(Guid.Parse(environment.Id), false, logger);
                    return;

                case CloudEnvironmentState.Starting:
                    // Starting -> Suspend
                    await EnvironmentSuspendAction.RunAsync(Guid.Parse(environment.Id), false, logger);
                    return;

                case CloudEnvironmentState.ShuttingDown:
                    // ShuttingDown -> Suspend
                    await EnvironmentSuspendAction.RunAsync(Guid.Parse(environment.Id), false, logger);
                    return;

                case CloudEnvironmentState.Provisioning:
                    // Provisioning -> Fail environment
                    await EnvironmentFailAction.RunAsync(Guid.Parse(environment.Id), EnvironmentMonitorConstants.EnvironmentRepairReason, logger);
                    return;

                case CloudEnvironmentState.Queued:
                   
                    if (environment.LastStateUpdateTrigger.Equals(CloudEnvironmentStateUpdateTriggers.CreateEnvironment, StringComparison.OrdinalIgnoreCase))
                    {
                        // Stay in Queued state when creating new environment -> Fail environment
                        await EnvironmentFailAction.RunAsync(Guid.Parse(environment.Id), EnvironmentMonitorConstants.EnvironmentRepairReason, logger);
                    }
                    else
                    {
                        // Otherwise -> Suspend environment
                        await EnvironmentSuspendAction.RunAsync(Guid.Parse(environment.Id), false, logger);
                    }
                    
                    return;

                case CloudEnvironmentState.Failed:
                    // Failed -> Delete
                    if (!environment.IsDeleted)
                    {
                        await EnvironmentDeleteAction.RunAsync(Guid.Parse(environment.Id), logger);
                    }

                    return;

                default:
                    return;
            }
        }
    }
}
