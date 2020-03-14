// <copyright file="IBackplaneManagerBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.SignalService
{
    public interface IBackplaneManagerBase
    {
        /// <summary>
        /// Run a long running task to update metrics and purge.
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        Task RunAsync(CancellationToken stoppingToken);

        /// <summary>
        /// Dispose of the backplane manager.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task DisposeAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Start tracking a data change that later will be purged.
        /// </summary>
        /// <param name="dataChanged">The data change being tracked.</param>
        /// <returns>True if this data changed is already known.</returns>
        bool TrackDataChanged(DataChanged dataChanged);
    }

    public interface IBackplaneManagerBase<TBackplaneProvider, TBackplaneProviderSupportLevel, TServiceMetrics> : IBackplaneManagerBase
        where TBackplaneProvider : IBackplaneProviderBase<TServiceMetrics>
        where TBackplaneProviderSupportLevel : BackplaneProviderSupportLevelBase
        where TServiceMetrics : struct
    {
        /// <summary>
        /// Gets the backplane providers.
        /// </summary>
        IReadOnlyCollection<TBackplaneProvider> BackplaneProviders { get; }

        /// <summary>
        /// Gets or Sets the metrics factory callback.
        /// </summary>
        Func<((string ServiceId, string Stamp), TServiceMetrics)> MetricsFactory { get; set; }

        /// <summary>
        /// Register a new provider.
        /// </summary>
        /// <param name="backplaneProvider"></param>
        /// <param name="supportCapabilities">Optional supported capabilities.</param>
        void RegisterProvider(TBackplaneProvider backplaneProvider, TBackplaneProviderSupportLevel supportCapabilities = null);

        /// <summary>
        /// Update metrics reported by a contact service.
        /// </summary>
        /// <param name="serviceInfo">Info on the service being reported</param>
        /// <param name="metrics">Metrics to report</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task UpdateBackplaneMetrics(
            (string ServiceId, string Stamp) serviceInfo,
            TServiceMetrics metrics,
            CancellationToken cancellationToken);
    }
}
