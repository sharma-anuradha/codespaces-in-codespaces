// <copyright file="BackplaneManagerBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService;
using Microsoft.VsCloudKernel.SignalService.Common;

namespace Microsoft.VsCloudKernel.Services.Backplane.Common
{
    public abstract class BackplaneManagerBase<TBackplaneProvider, TBackplaneProviderSupportLevel, TServiceMetrics>
        where TBackplaneProvider : IBackplaneProviderBase<TServiceMetrics>
        where TBackplaneProviderSupportLevel : BackplaneProviderSupportLevelBase
        where TServiceMetrics : struct
    {
        private const string MethodDisposeDataChanges = nameof(DisposeDataChangesAsync);
        private const string MethodUpdateBackplaneMetrics = nameof(UpdateBackplaneMetricsAsync);
        private const string MethodDisposeExpiredDataChangesAsync = nameof(DisposeExpiredDataChangesAsync);

        private readonly object backplaneProvidersLock = new object();
        private readonly Dictionary<TBackplaneProvider, TBackplaneProviderSupportLevel> backplaneProviders = new Dictionary<TBackplaneProvider, TBackplaneProviderSupportLevel>();
        private readonly Dictionary<string, (DateTime, DataChanged, bool)> backplaneChanges = new Dictionary<string, (DateTime, DataChanged, bool)>();
        private readonly object backplaneChangesLock = new object();

        protected BackplaneManagerBase(ILogger logger, IDataFormatProvider formatProvider = null)
        {
            Logger = Requires.NotNull(logger, nameof(logger));
            FormatProvider = formatProvider;
        }

        public Func<(ServiceInfo, TServiceMetrics)> MetricsFactory { get; set; }

        /// <summary>
        /// Gets the list of backplane providers, note this is a thread safe property.
        /// </summary>
        public IReadOnlyCollection<TBackplaneProvider> BackplaneProviders
        {
            get
            {
                lock (this.backplaneProvidersLock)
                {
                    return this.backplaneProviders.Keys.ToArray();
                }
            }
        }

        public int BackplaneChangesCount
        {
            get
            {
                lock (this.backplaneChangesLock)
                {
                    return this.backplaneChanges.Count;
                }
            }
        }

        protected IDataFormatProvider FormatProvider { get; }

        protected ILogger Logger { get; }

        public async Task HandleNextAsync(bool updateBackplaneMetrics, CancellationToken stoppingToken)
        {
            if (updateBackplaneMetrics)
            {
                await UpdateBackplaneMetricsAsync(stoppingToken);
            }

            // purge data changes
            await DisposeExpiredDataChangesAsync(stoppingToken);
        }

        public async Task DisposeAsync(CancellationToken cancellationToken)
        {
            Logger.LogDebug($"Dispose");

            DataChanged[] allDataChanges = null;
            lock (this.backplaneChangesLock)
            {
                allDataChanges = this.backplaneChanges.Select(i => i.Value.Item2).ToArray();
                this.backplaneChanges.Clear();
            }

            await DisposeDataChangesAsync(allDataChanges, cancellationToken);

            // attempt to dispose all providers
            foreach (var disposable in BackplaneProviders.OfType<IAsyncDisposable>())
            {
                await disposable.DisposeAsync();
            }
        }

        public void RegisterProvider(TBackplaneProvider backplaneProvider, TBackplaneProviderSupportLevel supportCapabilities = null)
        {
            Logger.LogInformation($"AddBackplaneProvider type:{backplaneProvider.GetType().FullName} supportCapabilities:{Newtonsoft.Json.JsonConvert.SerializeObject(supportCapabilities)}");
            lock (this.backplaneProvidersLock)
            {
                this.backplaneProviders.Add(backplaneProvider, supportCapabilities);
            }

            OnRegisterProvider(backplaneProvider);
        }

        public async Task UpdateBackplaneMetricsAsync(
            ServiceInfo serviceInfo,
            TServiceMetrics metrics,
            CancellationToken cancellationToken)
        {
            await WaitAll(
                GetSupportedProviders(s => s.UpdateMetrics).Select(p => (p.UpdateMetricsAsync(serviceInfo, metrics, cancellationToken), p)),
                nameof(UpdateBackplaneMetricsAsync),
                $"serviceId:{serviceInfo.ServiceId}");
        }

        public async Task DisposeExpiredDataChangesAsync(CancellationToken cancellationToken)
        {
            const int SecondsExpired = 60;
            var expiredThreshold = DateTime.Now.Subtract(TimeSpan.FromSeconds(SecondsExpired));

            KeyValuePair<string, (DateTime, DataChanged, bool)>[] expiredCacheItems = null;
            lock (this.backplaneChangesLock)
            {
                // Note: next block will remove the 'stale' changes
                var possibleExpiredCacheItems = this.backplaneChanges.Where(kvp => !kvp.Value.Item3 && kvp.Value.Item1 < expiredThreshold);

                if (possibleExpiredCacheItems.Any())
                {
                    expiredCacheItems = possibleExpiredCacheItems.ToArray();
                    foreach (var key in expiredCacheItems.Select(i => i.Key))
                    {
                        this.backplaneChanges.Remove(key);
                    }
                }
            }

            if (expiredCacheItems?.Length > 0)
            {
                try
                {
                    // have the backplane providers to dispose this items
                    await DisposeDataChangesAsync(expiredCacheItems.Select(i => i.Value.Item2).ToArray(), cancellationToken);
                }
                catch (Exception error)
                {
                    Logger.LogMethodScope(LogLevel.Error, error, "Failed to dispose expired data changes", MethodDisposeExpiredDataChangesAsync);
                }
            }
        }

        public bool HasTrackDataChanged(DataChanged dataChanged)
        {
            lock (this.backplaneChangesLock)
            {
                return this.backplaneChanges.ContainsKey(dataChanged.ChangeId);
            }
        }

        public bool TrackDataChanged(DataChanged dataChanged, TrackDataChangedOptions options = TrackDataChangedOptions.None)
        {
            lock (this.backplaneChangesLock)
            {
                if (options.HasFlag(TrackDataChangedOptions.ForceRemove))
                {
                    return this.backplaneChanges.Remove(dataChanged.ChangeId);
                }

                (DateTime, DataChanged, bool) item;
                bool result = this.backplaneChanges.TryGetValue(dataChanged.ChangeId, out item);

                bool isLocked = options.HasFlag(TrackDataChangedOptions.Lock);
                if (result)
                {
                    if (options.HasFlag(TrackDataChangedOptions.Refresh))
                    {
                        item.Item1 = DateTime.Now;
                    }

                    if (options.HasFlag(TrackDataChangedOptions.Refresh) || item.Item3 != isLocked)
                    {
                        item.Item3 = isLocked;
                        this.backplaneChanges[dataChanged.ChangeId] = item;
                    }
                }
                else
                {
                    item = (DateTime.Now, dataChanged, isLocked);
                    this.backplaneChanges.Add(dataChanged.ChangeId, item);
                }

                return result;
            }
        }

        protected abstract void OnRegisterProvider(TBackplaneProvider backplaneProvider);

        protected abstract void AddMetricsScope(List<(string, object)> metricsScope, TServiceMetrics metrics);

        protected TBackplaneProvider[] GetSupportedProviders(Func<TBackplaneProviderSupportLevel, int?> capabilityCallback)
        {
            Func<int?, int> priorityCallback = (value) => value.HasValue ? value.Value : BackplaneProviderSupportLevelConst.DefaultSupportThreshold;

            Dictionary<TBackplaneProvider, int> providersByPriority;
            lock (this.backplaneProvidersLock)
            {
                providersByPriority = this.backplaneProviders.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value == null ? BackplaneProviderSupportLevelConst.DefaultSupportThreshold : priorityCallback(capabilityCallback(kvp.Value)));
            }

            var supportedProviders = providersByPriority.Where(kvp => kvp.Value > BackplaneProviderSupportLevelConst.NoSupportThreshold);

            // if we have more that 1 provider we can safely discard the one with minimum support
            if (supportedProviders.Count() > 1)
            {
                // we restrict only on non minimal supported
                supportedProviders = supportedProviders.Where(kvp => kvp.Value > BackplaneProviderSupportLevelConst.MinimumSupportThreshold);
            }

            return supportedProviders
                .OrderByDescending(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .ToArray();
        }

        protected async Task WaitAll(
            IEnumerable<(Task, TBackplaneProvider)> tasks,
            string methodName,
            string logText)
        {
            var start = Stopwatch.StartNew();
            var taskItems = tasks.Select(i => (i.Item1, i.Item2, Stopwatch.StartNew())).ToList();
            var completedItems = new List<(Task, TBackplaneProvider, Stopwatch)>();

            while (taskItems.Count > 0)
            {
                var t = await Task.WhenAny(taskItems.Select(i => i.Item1));
                var taskItem = taskItems.First(i => i.Item1 == t);
                taskItem.Item3.Stop();
                completedItems.Add(taskItem);
                taskItems.Remove(taskItem);
                try
                {
                    await t;
                }
                catch (Exception error)
                {
                    HandleBackplaneProviderException(taskItem.Item2, methodName, error);
                }
            }

            // all the tasks completed or failed
            var perTaskElapsed = completedItems.Select(i => $"{i.Item2.GetType().Name}:{i.Item3.ElapsedMilliseconds}");
            Logger.LogScope(
                LogLevel.Debug,
                $"{logText} -> [{string.Join(",", perTaskElapsed)}]",
                (LoggerScopeHelpers.MethodScope, methodName),
                (LoggerScopeHelpers.MethodPerfScope, start.ElapsedMilliseconds));
        }

        protected async Task<T> WaitFirstOrDefault<T>(
            IEnumerable<(Task<T>, TBackplaneProvider)> tasks,
            string methodName,
            Func<T, string> logTextCallback,
            Func<T, bool> resultCallback = null)
        {
            var start = Stopwatch.StartNew();
            var taskItems = tasks.Select(i => (i.Item1, i.Item2, Stopwatch.StartNew())).ToList();
            var completedItems = new List<(Task, TBackplaneProvider, Stopwatch)>();

            T resultT = default(T);
            while (taskItems.Count > 0)
            {
                var t = await Task.WhenAny(taskItems.Select(i => i.Item1));
                var taskItem = taskItems.First(i => i.Item1 == t);
                taskItem.Item3.Stop();
                completedItems.Add(taskItem);
                taskItems.Remove(taskItem);
                try
                {
                    var result = await t;
                    if (resultCallback == null || resultCallback(result))
                    {
                        resultT = result;
                        break;
                    }
                }
                catch (Exception error)
                {
                    HandleBackplaneProviderException(taskItem.Item2, methodName, error);
                }
            }

            var perTaskElapsed = completedItems.Select(i => $"{i.Item2.GetType().Name}:{i.Item3.ElapsedMilliseconds}");
            Logger.LogScope(
                LogLevel.Debug,
                $"{logTextCallback(resultT)} -> [{string.Join(",", perTaskElapsed)}]",
                (LoggerScopeHelpers.MethodScope, methodName),
                (LoggerScopeHelpers.MethodPerfScope, start.ElapsedMilliseconds));

            return resultT;
        }

        protected string ToTraceText(string s)
        {
            return string.Format(FormatProvider, "{0:T}", s);
        }

        /// <summary>
        /// Return true when this type of exception should be logged as an error to report in our telemetry.
        /// </summary>
        /// <param name="error">The error instance.</param>
        /// <returns>True if we should log this exception.</returns>
        private static bool ShouldLogException(Exception error)
        {
            return !(
                error is BackplaneNotAvailableException ||
                error is OperationCanceledException ||
                error.GetType().Name == "ServiceUnavailableException");
        }

        private async Task UpdateBackplaneMetricsAsync(CancellationToken cancellationToken)
        {
            if (MetricsFactory != null)
            {
                var metrics = MetricsFactory();

                try
                {
                    // update metrics
                    await UpdateBackplaneMetricsWithLoggingAsync(metrics.Item1, metrics.Item2, cancellationToken);
                }
                catch (Exception error)
                {
                    Logger.LogMethodScope(LogLevel.Error, error, "Failed to update backplane metrics", MethodUpdateBackplaneMetrics);
                }
            }
        }

        private async Task UpdateBackplaneMetricsWithLoggingAsync(
            ServiceInfo serviceInfo,
            TServiceMetrics metrics,
            CancellationToken cancellationToken)
        {
            var metricsScope = new List<(string, object)>();
            metricsScope.Add((LoggerScopeHelpers.MethodScope, MethodUpdateBackplaneMetrics));
            metricsScope.Add((BackplaneManagerConst.BackplaneChangesCountProperty, BackplaneChangesCount));
            AddMetricsScope(metricsScope, metrics);

            using (LoggerScopeHelpers.BeginScope(Logger, metricsScope.ToArray()))
            {
                Logger.LogInformation($"Metrics for serviceType:{serviceInfo.ServiceType}");
            }

            await UpdateBackplaneMetricsAsync(serviceInfo, metrics, cancellationToken);
        }

        private async Task DisposeDataChangesAsync(
            DataChanged[] dataChanges,
            CancellationToken cancellationToken)
        {
            await WaitAll(
                GetSupportedProviders(s => s.DisposeDataChanges).Select(p => (p.DisposeDataChangesAsync(dataChanges, cancellationToken), p)),
                nameof(IBackplaneProviderBase<TServiceMetrics>.DisposeDataChangesAsync),
                $"size:{dataChanges.Length}");
        }

        private void HandleBackplaneProviderException(TBackplaneProvider backplaneProvider, string methodName, Exception error)
        {
            if (!backplaneProvider.HandleException(methodName, error))
            {
                if (ShouldLogException(error))
                {
                    Logger.LogWarning(error, $"Failed to invoke method:{methodName} on provider:{backplaneProvider.GetType().Name}");
                }
            }
        }
    }
}
