// <copyright file="JobSchedulerLeaseProviderBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Base class to implement IJobSchedulerLeaseProvider.
    /// </summary>
    public abstract class JobSchedulerLeaseProviderBase : IJobSchedulerLeaseProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JobSchedulerLeaseProviderBase"/> class.
        /// </summary>
        /// <param name="claimedDistributedLease">A claim distributed lease instance.</param>
        /// <param name="resourceNameBuilder">A resource name builder instance.</param>
        protected JobSchedulerLeaseProviderBase(
            IClaimedDistributedLease claimedDistributedLease,
            IResourceNameBuilder resourceNameBuilder)
        {
            ClaimedDistributedLease = Requires.NotNull(claimedDistributedLease, nameof(claimedDistributedLease));
            ResourceNameBuilder = Requires.NotNull(resourceNameBuilder, nameof(resourceNameBuilder));
        }

        /// <summary>
        /// Gets the lease container to use on the distributed lease.
        /// </summary>
        protected abstract string LeaseContainerName { get; }

        private IClaimedDistributedLease ClaimedDistributedLease { get; }

        private IResourceNameBuilder ResourceNameBuilder { get; }

        /// <inheritdoc/>
        public Task<IDisposable> ObtainAsync(string jobName, TimeSpan timeSpan, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            return ClaimedDistributedLease.Obtain(
                LeaseContainerName,
                ResourceNameBuilder.GetLeaseName($"schedule-{jobName}-lease"),
                timeSpan,
                logger);
        }
    }
}
