// <copyright file="SuspendEnvironmentJobHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers
{
    /// <summary>
    /// Soft Delete environments handler.
    /// </summary>
    public class SuspendEnvironmentJobHandler : JobHandlerPayloadBase<SuspendEnvironmentPayload>, IJobHandlerTarget
    {
        /// <summary>
        /// Gets default target name for item on queue.
        /// </summary>
        public const string DefaultQueueId = "jobhandler-suspend-environment";

        /// <summary>
        /// Initializes a new instance of the <see cref="SuspendEnvironmentJobHandler"/> class.
        /// </summary>
        /// <param name="environmentManager">Environment manager instance.</param>
        /// <param name="currentIdentityProvider">Target identity provider.</param>
        /// <param name="superuserIdentity">Target super user identity.</param>
        public SuspendEnvironmentJobHandler(
            IEnvironmentManager environmentManager,
            ICurrentIdentityProvider currentIdentityProvider,
            VsoSuperuserClaimsIdentity superuserIdentity)
        {
            EnvironmentManager = environmentManager;
            CurrentIdentityProvider = currentIdentityProvider;
            SuperuserIdentity = superuserIdentity;
        }

        public static Task ExecuteAsync(IJobQueueProducerFactory jobQueueProducerFactory, string environmentId, AzureLocation location, IDiagnosticsLogger logger)
        {
            Requires.NotNull(jobQueueProducerFactory, nameof(jobQueueProducerFactory));

            return jobQueueProducerFactory.GetOrCreate(DefaultQueueId, location).AddJobAsync(
                new SuspendEnvironmentPayload() { EnvironmentId = environmentId },
                null,
                logger.NewChildLogger(),
                default);
        }

        private IEnvironmentManager EnvironmentManager { get; }

        private ICurrentIdentityProvider CurrentIdentityProvider { get; }

        private VsoSuperuserClaimsIdentity SuperuserIdentity { get; }

        /// <inheritdoc/>
        public IJobHandler JobHandler => this;

        /// <inheritdoc/>
        public string QueueId => DefaultQueueId;

        /// <inheritdoc/>
        public AzureLocation? Location => null;

        /// <inheritdoc/>
        protected override Task HandleJobAsync(SuspendEnvironmentPayload payload, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            return logger.OperationScopeAsync("handle_environment_suspension", async (childLogger) =>
            {
                childLogger.AddEnvironmentId(payload.EnvironmentId);

                try
                {
                    using (CurrentIdentityProvider.SetScopedIdentity(SuperuserIdentity))
                    {
                        await EnvironmentManager.SuspendAsync(Guid.Parse(payload.EnvironmentId), childLogger.NewChildLogger());
                    }
                }
                catch (Exception ex)
                {
                    childLogger.LogException("handle_environment_suspension_error", ex);
                }
            });
        }
    }
}
