// <copyright file="EnvironmentTaskBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Tasks
{
    /// <summary>
    /// Base type for all Environment tasks.
    /// </summary>
    public abstract class EnvironmentTaskBase : BaseBackgroundTask, IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentTaskBase"/> class.
        /// </summary>
        /// <param name="environmentManagerSettings">Used for lease container name.</param>
        /// <param name="cloudEnvironmentRepository">Used for all environment manager sub queries.</param>
        /// <param name="taskHelper">the task helper.</param>
        /// <param name="claimedDistributedLease"> used to create leases.</param>
        /// <param name="resourceNameBuilder">Used to build the lease name.</param>
        /// <param name="configurationReader">Configuration reader.</param>
        /// <param name="jobSchedulerFeatureFlags">job queue feature flag</param>
        public EnvironmentTaskBase(
            EnvironmentManagerSettings environmentManagerSettings,
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            ITaskHelper taskHelper,
            IClaimedDistributedLease claimedDistributedLease,
            IResourceNameBuilder resourceNameBuilder,
            IJobSchedulerFeatureFlags jobSchedulerFeatureFlags,
            IConfigurationReader configurationReader)
            : base(configurationReader)
        {
            EnvironmentManagerSettings = environmentManagerSettings;
            CloudEnvironmentRepository = cloudEnvironmentRepository;
            TaskHelper = taskHelper;
            ClaimedDistributedLease = claimedDistributedLease;
            ResourceNameBuilder = resourceNameBuilder;
            JobSchedulerFeatureFlags = jobSchedulerFeatureFlags;
        }

        /// <summary>
        /// Gets a <see cref="ITaskHelper"/> used to schedule tasks.
        /// </summary>
        protected ITaskHelper TaskHelper { get; }

        /// <summary>
        /// Gets an <see cref="IClaimedDistributedLease"/> used to get leases by the tasks that need to.
        /// </summary>
        protected IClaimedDistributedLease ClaimedDistributedLease { get; }

        /// <summary>
        /// Gets a <see cref="IResourceNameBuilder"/> used to build lease names.
        /// </summary>
        protected IResourceNameBuilder ResourceNameBuilder { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the type has been disposed.
        /// </summary>
        protected bool Disposed { get; set; }

        /// <summary>
        /// Gets the settings used to get the lease container name.
        /// </summary>
        protected EnvironmentManagerSettings EnvironmentManagerSettings { get; }

        /// <summary>
        /// Gets the cloud env repository which is used for most queries.
        /// </summary>
        protected ICloudEnvironmentRepository CloudEnvironmentRepository { get; }

        /// <summary>
        /// Feature flag name
        /// </summary>
        protected virtual string FeatureFlagName => "EnvironmentManagerJob";

        /// <summary>
        /// Is deprecated
        /// </summary>
        protected virtual bool IsDeprecated => false;

        private IJobSchedulerFeatureFlags JobSchedulerFeatureFlags { get; }

        /// <inheritdoc/>
        public override void Dispose()
        {
            Disposed = true;
        }

        /// <summary>
        /// Obtains a lease with a given name for a given timerange.
        /// </summary>
        /// <param name="leaseName">the name of the lease being sought.</param>
        /// <param name="claimSpan">the duration for the lease.</param>
        /// <param name="logger">the logger.</param>
        /// <returns>a task contained the results of obtaining the lease.</returns>
        protected async Task<IDisposable> ObtainLeaseAsync(string leaseName, TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            if (IsDeprecated && await JobSchedulerFeatureFlags.IsFeatureFlagEnabledAsync(this.FeatureFlagName, IsDeprecated))
            {
                return null;
            }

            return ClaimedDistributedLease.Obtain(
                EnvironmentManagerSettings.LeaseContainerName, leaseName, claimSpan, logger);
        }
    }
}
