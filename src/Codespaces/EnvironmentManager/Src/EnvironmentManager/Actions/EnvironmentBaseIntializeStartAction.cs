// <copyright file="EnvironmentBaseIntializeStartAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Actions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Actions
{
    /// <summary>
    /// Environment Intialize Start Action.
    /// </summary>
    /// <typeparam name="TInput">Input type.</typeparam>
    public abstract class EnvironmentBaseIntializeStartAction<TInput> : EnvironmentItemAction<TInput, object>, IEnvironmentBaseIntializeStartAction<TInput>
    where TInput : EnvironmentBaseStartActionInput
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentBaseIntializeStartAction{TInput}"/> class.
        /// </summary>
        /// <param name="environmentStateManager">Target environment state manager.</param>
        /// <param name="repository">Target repository.</param>
        /// <param name="currentLocationProvider">Target current location provider.</param>
        /// <param name="currentUserProvider">Target current user provider.</param>
        /// <param name="controlPlaneInfo">Target control plane info.</param>
        /// <param name="environmentAccessManager">Target environment access manager.</param>
        /// <param name="skuCatalog">Target sku catalog.</param>
        /// <param name="skuUtils">Target skuUtils, to find sku's eligiblity.</param>
        protected EnvironmentBaseIntializeStartAction(
            IEnvironmentStateManager environmentStateManager,
            ICloudEnvironmentRepository repository,
            ICurrentLocationProvider currentLocationProvider,
            ICurrentUserProvider currentUserProvider,
            IControlPlaneInfo controlPlaneInfo,
            IEnvironmentAccessManager environmentAccessManager,
            ISkuCatalog skuCatalog,
            ISkuUtils skuUtils)
            : base(
                  environmentStateManager,
                  repository,
                  currentLocationProvider,
                  currentUserProvider,
                  controlPlaneInfo,
                  environmentAccessManager,
                  skuCatalog,
                  skuUtils)
        {
        }

        /// <summary>
        /// Configures Run Core Async.
        /// </summary>
        /// <param name="record"> Environment transition record. </param>
        /// <param name="logger"> Logger. </param>
        /// <returns>True if the action is good to proceed, false otherwise..</returns>
        protected bool ConfigureRunCore(
            EnvironmentTransition record,
            IDiagnosticsLogger logger)
        {
            // No action required if the environment is already running
            if (record.Value.State == CloudEnvironmentState.Available)
            {
                return false;
            }

            // No action required if the environment is already in target state
            if (IsEnvironmentInTargetState(record.Value.State))
            {
                return false;
            }

            ValidateEnvironment(record.Value);

            // Authorize
            EnvironmentAccessManager.AuthorizeEnvironmentAccess(record.Value, nonOwnerScopes: null, logger);

            return true;
        }

        /// <inheritdoc/>
        protected override async Task<bool> HandleExceptionAsync(
            TInput input,
            Exception ex,
            object transientState,
            IDiagnosticsLogger logger)
        {
            // Stop if the record in the database is already in target state
            if (ex is ConflictException ce && ce.MessageCode == (int)CommonMessageCodes.ConcurrentModification)
            {
                var record = await FetchAsync(input, logger);
                if (IsEnvironmentInTargetState(record.Value.State))
                {
                    logger.AddReason("Already being started.");
                }
            }

            // No further actions required, return exception to client.
            return false;
        }

        /// <summary>
        /// Update environment state and save the record.
        /// </summary>
        /// <param name="record">Target environment entity transition record.</param>
        /// <param name="targetState">Target state.</param>
        /// <param name="reason">State change reason.</param>
        /// <param name="trigger">State change trigger.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected async Task UpdateStateAsync(
            EnvironmentTransition record,
            CloudEnvironmentState targetState,
            string reason,
            string trigger,
            IDiagnosticsLogger logger)
        {
            await EnvironmentStateManager.SetEnvironmentStateAsync(
                                record,
                                targetState,
                                trigger,
                                reason,
                                null,
                                logger);

            // Apply transitions and persist the environment to database
            await Repository.UpdateTransitionAsync("cloudenvironment", record, logger);
        }

        /// <summary>
        /// Validate environment.
        /// </summary>
        /// <param name="environment">Target environment.</param>
        protected virtual void ValidateEnvironment(CloudEnvironment environment)
        {
            // Cannot start an environment that is not suspended
            if (!environment.IsShutdown())
            {
                throw new CodedValidationException((int)MessageCodes.EnvironmentNotShutdown);
            }
        }

        /// <summary>
        /// Check whether the environment is in target state.
        /// </summary>
        /// <param name="cloudEnvironmentState">Current state.</param>
        /// <returns>True if environment is in target state, false otherwise.</returns>
        protected abstract bool IsEnvironmentInTargetState(CloudEnvironmentState cloudEnvironmentState);
    }
}
