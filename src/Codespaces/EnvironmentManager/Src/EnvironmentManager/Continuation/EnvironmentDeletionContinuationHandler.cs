// <copyright file="EnvironmentDeletionContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;

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
        /// <param name="serviceProvider">Dependency Injection service provider.</param>
        public EnvironmentDeletionContinuationHandler(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        private IEnvironmentManager EnvironmentManager
        {
            // Workaround for circular dependency that prevents constructor injection of EnvironmentManager.
            get { return ServiceProvider.GetRequiredService<IEnvironmentManager>(); }
        }

        private IServiceProvider ServiceProvider { get; }

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
                var environmentDeletionInput = input as EnvironmentContinuationInput;

                if (environmentDeletionInput != null)
                {
                    var environment = await EnvironmentManager.GetAsync(environmentDeletionInput.EnvironmentId, logger.NewChildLogger());
                    if (environment != null)
                    {
                        childLogger.AddCloudEnvironment(environment);
                        var isDeleted = await EnvironmentManager.DeleteAsync(environment, logger.NewChildLogger());
                        if (isDeleted)
                        {
                            return CreateFinalResult(OperationState.Succeeded);
                        }

                        return CreateFinalResult(OperationState.Failed, "DeletionFailed");
                    }

                    return CreateFinalResult(OperationState.Succeeded);
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
