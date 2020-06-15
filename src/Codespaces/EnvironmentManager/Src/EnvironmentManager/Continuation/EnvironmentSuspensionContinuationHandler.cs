// <copyright file="EnvironmentSuspensionContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Continuation
{
    /// <summary>
    /// Suspention environments handler.
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
        public EnvironmentSuspensionContinuationHandler(IServiceProvider serviceProvider)
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
            return await logger.OperationScopeAsync("handle_environment_suspension", async (childLogger) =>
            {
                var environmentSuspensionInput = input as EnvironmentContinuationInput;

                if (environmentSuspensionInput != null)
                {
                    var environment = await EnvironmentManager.GetAsync(environmentSuspensionInput.EnvironmentId, logger.NewChildLogger());
                    if (environment != null)
                    {
                        childLogger.AddCloudEnvironment(environment);
                        var isSuspended = await EnvironmentManager.SuspendAsync(environment, logger.NewChildLogger());
                        if (isSuspended.HttpStatusCode == StatusCodes.Status200OK)
                        {
                            return CreateFinalResult(OperationState.Succeeded);
                        }

                        return CreateFinalResult(OperationState.Failed, "SuspensionFailed");
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
