// <copyright file="EnvironmentSuspensionContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Continuation
{
    /// <summary>
    /// Suspension environments handler.
    /// </summary>
    public class EnvironmentSuspensionContinuationHandler : IContinuationTaskMessageHandler
    {
        /// <summary>
        /// Gets default target name for item on queue.
        /// </summary>
        public const string DefaultQueueTarget = "EnvironmentSuspentionHandler";

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentSuspensionContinuationHandler"/> class.
        /// </summary>
        /// <param name="serviceProvider">Dependency Injection service provider.</param>
        /// <param name="currentIdentityProvider">Target identity provider.</param>
        /// <param name="superuserIdentity">Target super user identity.</param>
        public EnvironmentSuspensionContinuationHandler(IServiceProvider serviceProvider, ICurrentIdentityProvider currentIdentityProvider, VsoSuperuserClaimsIdentity superuserIdentity)
        {
            ServiceProvider = serviceProvider;
            CurrentIdentityProvider = currentIdentityProvider;
            SuperuserIdentity = superuserIdentity;
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
            return await logger.OperationScopeAsync("handle_environment_suspension", async (childLogger) =>
            {
                var environmentSuspensionInput = input as EnvironmentContinuationInput;
                childLogger.AddEnvironmentId(environmentSuspensionInput.EnvironmentId);

                if (environmentSuspensionInput != null)
                {
                    try
                    {
                        using (CurrentIdentityProvider.SetScopedIdentity(SuperuserIdentity))
                        {
                            await EnvironmentManager.SuspendAsync(Guid.Parse(environmentSuspensionInput.EnvironmentId), logger.NewChildLogger());
                        }

                        return CreateFinalResult(OperationState.Succeeded);
                    }
                    catch (Exception ex)
                    {
                        childLogger.LogException("handle_environment_suspension_error", ex);
                        return CreateFinalResult(OperationState.Failed, "SuspensionFailed");
                    }
                }

                return CreateFinalResult(OperationState.Failed, "InvalidInputType");
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
