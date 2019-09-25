// <copyright file="IClaimedDistributedLease.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Diagnostics;
using System;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions
{
    /// <summary>
    /// Allows components to obtain a distributed lease lock. This allows for distributed
    /// components to know whether another component is working on a given resource and
    /// either wait for it to be freed, or move on. This specific lease, only allows
    /// one lease to be obtained within a given "claim period". This supports the notion
    /// of having operations that can only happen once per day.
    /// </summary>
    public interface IClaimedDistributedLease
    {
        /// <summary>
        /// Attempts to obtain a lease on a given name. If the lease can not be obtained, will
        /// return null. Specifically,  it only allows one lease to be obtained within a given
        /// "claim period". This supports the notion of having operations that can only happen
        /// once per day. Here, if a lease hasn't been obtained since the `claimPeriod`, then
        /// the claim lease will be given (i.e. if (lastLeaseTime < claimPeriod) { /* give
        /// lease */ }. So `claimPeriod` should represent the start of period (i.e. the start 
        /// of the day if you only want one operation per day).
        /// </summary>
        /// <param name="containerName">Blob container name that should be targeted.</param>
        /// <param name="name">The name of the lease that should be obtained.</param>
        /// <param name="timeSpan">Target time span.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Disposable that will automatically release the lock when when disposed.
        /// If null, lock could not be obtained.</returns>
        Task<IDisposable> Obtain(string containerName, string name, TimeSpan timeSpan, IDiagnosticsLogger logger);
    }
}