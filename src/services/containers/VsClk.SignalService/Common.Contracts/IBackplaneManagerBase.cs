// <copyright file="IBackplaneManagerBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.SignalService
{
    [Flags]
    public enum TrackDataChangedOptions
    {
        /// <summary>
        /// None option.
        /// </summary>
        None = 0,

        /// <summary>
        /// Lock data changes, disable expiration
        /// </summary>
        Lock = 1,

        /// <summary>
        /// Refresh the expiration timestamp
        /// </summary>
        Refresh = 2,

        /// <summary>
        /// Cause to remove the tracked change
        /// </summary>
        ForceRemove,
    }

    public interface IBackplaneManagerBase
    {
        /// <summary>
        /// Gets the actual backplane changes count.
        /// </summary>
        int BackplaneChangesCount { get; }

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
        /// <param name="options">Flags to control the behavior of the tracked changes.</param>
        /// <returns>True if this data changed is already known.</returns>
        bool TrackDataChanged(DataChanged dataChanged, TrackDataChangedOptions options = TrackDataChangedOptions.None);
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
        Func<((string ServiceId, string Stamp, string ServiceType), TServiceMetrics)> MetricsFactory { get; set; }

        /// <summary>
        /// Register a new provider.
        /// </summary>
        /// <param name="backplaneProvider"></param>
        /// <param name="supportCapabilities">Optional supported capabilities.</param>
        void RegisterProvider(TBackplaneProvider backplaneProvider, TBackplaneProviderSupportLevel supportCapabilities = null);

        /// <summary>
        /// Update metrics reported by a contact service.
        /// </summary>
        /// <param name="serviceInfo">Info on the service being reported.</param>
        /// <param name="metrics">Metrics to report.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task UpdateBackplaneMetricsAsync(
            (string ServiceId, string Stamp, string ServiceType) serviceInfo,
            TServiceMetrics metrics,
            CancellationToken cancellationToken);
    }
}
