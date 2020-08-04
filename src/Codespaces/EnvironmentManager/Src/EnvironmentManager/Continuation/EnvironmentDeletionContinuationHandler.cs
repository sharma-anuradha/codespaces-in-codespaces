// <copyright file="EnvironmentDeletionContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Continuation
{
    /// <summary>
    /// Delete environments handler.
    /// </summary>
    public class EnvironmentDeletionContinuationHandler : IContinuationTaskMessageHandler
    {
        /// <summary>
        /// Gets default target name for item on queue.
        /// </summary>
        public const string DefaultQueueTarget = "EnvironmentDeletionHandler";

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentDeletionContinuationHandler"/> class.
        /// </summary>
        /// <param name="environmentDeleteAction">Target environment delete action.</param>
        /// <param name="currentIdentityProvider">Target identity provider.</param>
        /// <param name="superuserIdentity">Target super user identity.</param>
        public EnvironmentDeletionContinuationHandler(
            IEnvironmentDeleteAction environmentDeleteAction,
            ICurrentIdentityProvider currentIdentityProvider,
            VsoSuperuserClaimsIdentity superuserIdentity)
        {
            EnvironmentDeleteAction = environmentDeleteAction;
            CurrentIdentityProvider = currentIdentityProvider;
            SuperuserIdentity = superuserIdentity;
        }

        private IEnvironmentDeleteAction EnvironmentDeleteAction { get; }

        private ICurrentIdentityProvider CurrentIdentityProvider { get; }

        private VsoSuperuserClaimsIdentity SuperuserIdentity { get; }

        /// <inheritdoc/>
        public bool CanHandle(ContinuationQueuePayload payload)
        {
            return payload.Target == DefaultQueueTarget;
        }

        /// <inheritdoc/>
        public async Task<ContinuationResult> Continue(ContinuationInput input, IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync("handle_environment_deletion", async (childLogger) =>
            {
                using (CurrentIdentityProvider.SetScopedIdentity(SuperuserIdentity))
                {
                    if (input is EnvironmentContinuationInput environmentDeletionInput)
                    {
                        childLogger.AddEnvironmentId(environmentDeletionInput.EnvironmentId);
                        var isDeleted = await EnvironmentDeleteAction.RunAsync(Guid.Parse(environmentDeletionInput.EnvironmentId), logger.NewChildLogger());
                        if (isDeleted)
                        {
                            return CreateFinalResult(OperationState.Succeeded);
                        }

                        return CreateFinalResult(OperationState.Failed, "DeletionFailed");
                    }

                    return CreateFinalResult(OperationState.Failed, "InvalidInputType");
                }
            });
        }

        private static ContinuationResult CreateFinalResult(OperationState state, string reason = default)
        {
            return new ContinuationResult
            {
                Status = state,
                RetryAfter = TimeSpan.Zero,
                NextInput = default,
                ErrorReason = reason,
            };
        }
    }
}
