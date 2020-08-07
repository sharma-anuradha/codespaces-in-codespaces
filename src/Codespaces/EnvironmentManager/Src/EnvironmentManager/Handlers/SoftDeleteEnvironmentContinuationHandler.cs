// <copyright file="SoftDeleteEnvironmentContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers
{
    /// <summary>
    /// Soft Delete environments handler.
    /// </summary>
    public class SoftDeleteEnvironmentContinuationHandler : IContinuationTaskMessageHandler
    {
        /// <summary>
        /// Gets default target name for item on queue.
        /// </summary>
        public const string DefaultQueueTarget = "EnvironmentDeletionHandler";

        /// <summary>
        /// Initializes a new instance of the <see cref="SoftDeleteEnvironmentContinuationHandler"/> class.
        /// </summary>
        /// <param name="serviceProvider">Dependency Injection service provider.</param>
        /// <param name="currentIdentityProvider">Target identity provider.</param>
        /// <param name="superuserIdentity">Target super user identity.</param>
        public SoftDeleteEnvironmentContinuationHandler(
            IServiceProvider serviceProvider,
            ICurrentIdentityProvider currentIdentityProvider,
            VsoSuperuserClaimsIdentity superuserIdentity)
        {
            CurrentIdentityProvider = currentIdentityProvider;
            SuperuserIdentity = superuserIdentity;
            ServiceProvider = serviceProvider;
    }

        private IEnvironmentManager EnvironmentManager
        {
            // Workaround for circular dependency that prevents constructor injection of EnvironmentManager.
            get { return ServiceProvider.GetRequiredService<IEnvironmentManager>(); }
        }

        private IServiceProvider ServiceProvider { get; }

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
            return await logger.OperationScopeAsync("handle_environment_soft_delete", async (childLogger) =>
            {
                using (CurrentIdentityProvider.SetScopedIdentity(SuperuserIdentity))
                {
                    if (input is EnvironmentContinuationInput environmentSoftDeleteInput)
                    {
                        childLogger.AddEnvironmentId(environmentSoftDeleteInput.EnvironmentId);
                        var cloudEnvironment = await EnvironmentManager.GetAsync(Guid.Parse(environmentSoftDeleteInput.EnvironmentId), logger.NewChildLogger());

                        if (cloudEnvironment == null)
                        {
                            return CreateFinalResult(OperationState.Cancelled, "EnvironmentNotFound");
                        }

                        if (cloudEnvironment.IsDeleted == true)
                        {
                            return CreateFinalResult(OperationState.Succeeded, "EnvironmentAlreadySoftDeleted");
                        }

                        var isDeleted = await EnvironmentManager.SoftDeleteAsync(Guid.Parse(cloudEnvironment.Id), logger.NewChildLogger());
                        if (isDeleted)
                        {
                            return CreateFinalResult(OperationState.Succeeded);
                        }

                        return CreateFinalResult(OperationState.Failed, "SoftDeletionFailed");
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
