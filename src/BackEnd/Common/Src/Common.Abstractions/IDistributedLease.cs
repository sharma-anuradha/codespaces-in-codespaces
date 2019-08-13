// <copyright file="IDistributedLease.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions
{
    /// <summary>
    /// Allows compnents to obtain a distribted lease lock. This allows for distributed
    /// components to know whether another componet is working on a given resource and
    /// either wait for it to be freed, or move on.
    /// </summary>
    public interface IDistributedLease
    {
        /// <summary>
        /// Attempts to obtain a lease on a given name. If the lease can not be obtained, will
        /// return null.
        /// </summary>
        /// <param name="containerName">Blob container name that should be targeted.</param>
        /// <param name="name">The name of the lease that should be obtained?</param>
        /// <returns>Disposable that will automatically release the lock when when disposed.
        /// If null, lock could not be obtained.</returns>
        Task<IDisposable> Obtain(string containerName, string name);

        /// <summary>
        /// Smilar to `Obtain` but will attempt to run 3 time waiting
        /// inbetween each attempt for the lease to available.
        /// </summary>
        /// <param name="containerName">Blob container name that should be targeted.</param>
        /// <param name="name">The name of the lease that should be obtained?</param>
        /// <returns>Disposable that will automatically release the lock when when disposed.
        /// If null, lock could not be obtained.</returns>
        Task<IDisposable> TryObtain(string containerName, string name);
    }
}
