// <copyright file="IBackplaneProviderBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.SignalService
{
    public interface IBackplaneProviderBase<TServiceMetrics>
    {
        /// <summary>
        /// Update the metrics of a service instance
        /// </summary>
        /// <param name="serviceInfo">Service Info</param>
        /// <param name="metrics">Metrics instance </param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task UpdateMetricsAsync((string ServiceId, string Stamp) serviceInfo, TServiceMetrics metrics, CancellationToken cancellationToken);

        /// <summary>
        /// Dispose a set of data changes that may have been notified by this provider
        /// </summary>
        /// <param name="dataChanges"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task DisposeDataChangesAsync(DataChanged[] dataChanges, CancellationToken cancellationToken);

        /// <summary>
        /// Invoked when an exception happen on some of the methods being invoked
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="error"></param>
        /// <returns>True if the backplane handle the exception by logging into telemetry or throwing a critical exception</returns>
        bool HandleException(string methodName, Exception error);
    }
}
