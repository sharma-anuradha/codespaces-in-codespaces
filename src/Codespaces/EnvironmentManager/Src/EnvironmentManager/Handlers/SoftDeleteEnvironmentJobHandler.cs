// <copyright file="SoftDeleteEnvironmentJobHandler.cs" company="Microsoft">
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
    public class SoftDeleteEnvironmentJobHandler : JobHandlerPayloadBase<SoftDeleteEnvironmentPayload>, IJobHandlerTarget
    {
        private const string ResultReasonProperty = "ResultReason";

        /// <summary>
        /// Gets default target name for item on queue.
        /// </summary>
        public const string DefaultQueueId = "jobhandler-delete-environment";

        /// <summary>
        /// Initializes a new instance of the <see cref="SoftDeleteEnvironmentJobHandler"/> class.
        /// </summary>
        /// <param name="environmentManager">Environment manager instance.</param>
        /// <param name="currentIdentityProvider">Target identity provider.</param>
        /// <param name="superuserIdentity">Target super user identity.</param>
        public SoftDeleteEnvironmentJobHandler(
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
                new SoftDeleteEnvironmentPayload() { EnvironmentId = environmentId },
                null,
                logger,
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
        protected override Task HandleJobAsync(SoftDeleteEnvironmentPayload payload, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            return logger.OperationScopeAsync("handle_environment_soft_delete", async (childLogger) =>
            {
                using (CurrentIdentityProvider.SetScopedIdentity(SuperuserIdentity))
                {
                    childLogger.AddEnvironmentId(payload.EnvironmentId);
                    var cloudEnvironment = await EnvironmentManager.GetAsync(Guid.Parse(payload.EnvironmentId), logger.NewChildLogger());

                    if (cloudEnvironment == null)
                    {
                        childLogger.AddValue(ResultReasonProperty, "EnvironmentNotFound");
                        return;
                    }

                    if (cloudEnvironment.IsDeleted == true)
                    {
                        childLogger.AddValue(ResultReasonProperty, "EnvironmentAlreadySoftDeleted");
                    }

                    var isDeleted = await EnvironmentManager.SoftDeleteAsync(Guid.Parse(cloudEnvironment.Id), logger.NewChildLogger());
                    if (!isDeleted)
                    {
                        childLogger.AddValue(ResultReasonProperty, "SoftDeletionFailed");
                    }
                }
            });
        }
    }
}
