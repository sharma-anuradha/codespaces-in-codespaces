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
        private const string MethodUpdateBackplaneMetrics = nameof(UpdateBackplaneMetrics);

        private readonly object backplaneProvidersLock = new object();
        private readonly Dictionary<TBackplaneProvider, TBackplaneProviderSupportLevel> backplaneProviders = new Dictionary<TBackplaneProvider, TBackplaneProviderSupportLevel>();
        private readonly Dictionary<string, (DateTime, DataChanged)> backplaneChanges = new Dictionary<string, (DateTime, DataChanged)>();
        private readonly object backplaneChangesLock = new object();

        protected BackplaneManagerBase(ILogger logger, IDataFormatProvider formatProvider = null)
        {
            Logger = Requires.NotNull(logger, nameof(logger));
            FormatProvider = formatProvider;
        }

        public Func<((string ServiceId, string Stamp), TServiceMetrics)> MetricsFactory { get; set; }

        /// <summary>
        /// Gets the list of backplane providers, note this is a thread safe property
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

        protected IDataFormatProvider FormatProvider { get; }

        protected ILogger Logger { get; }

        public async Task RunAsync(CancellationToken stoppingToken)
        {
            const int TimespanUpdateSecs = 5;

            Logger.LogDebug($"RunAsync");

            await UpdateBackplaneMetrics(stoppingToken);

            var updateMetricsCounter = new SecondsCounter(BackplaneManagerConst.UpdateMetricsSecs, TimespanUpdateSecs);
            while (true)
            {
                // update metrics if factory is defined
                if (updateMetricsCounter.Next())
                {
                    await UpdateBackplaneMetrics(stoppingToken);
                }

                // purge data changes (every 5 secs)
                await DisposeExpiredDataChangesAsync(null, stoppingToken);

                // delay
                await Task.Delay(TimeSpan.FromSeconds(TimespanUpdateSecs), stoppingToken);
            }
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
            foreach (var disposable in BackplaneProviders.OfType<VisualStudio.Threading.IAsyncDisposable>())
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

        public async Task UpdateBackplaneMetrics(
            (string ServiceId, string Stamp) serviceInfo,
            TServiceMetrics metrics,
            CancellationToken cancellationToken)
        {
            await WaitAll(
                GetSupportedProviders(s => s.UpdateMetrics).Select(p => (p.UpdateMetricsAsync(serviceInfo, metrics, cancellationToken), p)),
                nameof(UpdateBackplaneMetrics),
                $"serviceId:{serviceInfo.ServiceId}");
        }

        public async Task DisposeExpiredDataChangesAsync(int? maxCount, CancellationToken cancellationToken)
        {
            const int SecondsExpired = 60;
            var expiredThreshold = DateTime.Now.Subtract(TimeSpan.FromSeconds(SecondsExpired));

            KeyValuePair<string, (DateTime, DataChanged)>[] expiredCacheItems = null;
            lock (this.backplaneChangesLock)
            {
                // Note: next block will remove the 'stale' changes
                var possibleExpiredCacheItems = this.backplaneChanges.Where(kvp => kvp.Value.Item1 < expiredThreshold);
                if (maxCount.HasValue)
                {
                    possibleExpiredCacheItems = possibleExpiredCacheItems.Take(maxCount.Value);
                }

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
                // have the backplane providers to dispose this items
                await DisposeDataChangesAsync(expiredCacheItems.Select(i => i.Value.Item2).ToArray(), cancellationToken);
            }
        }

        public bool TrackDataChanged(DataChanged dataChanged)
        {
            lock (this.backplaneChangesLock)
            {
                if (this.backplaneChanges.ContainsKey(dataChanged.ChangeId))
                {
                    return true;
                }

                // track this data changed
                this.backplaneChanges.Add(dataChanged.ChangeId, (DateTime.Now, dataChanged));
                return false;
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
        /// Return true when this type of exception should be logged as an error to report in our telemetry
        /// </summary>
        /// <param name="error">The error instance</param>
        /// <returns></returns>
        private static bool ShouldLogException(Exception error)
        {
            return !(
                error is OperationCanceledException ||
                error.GetType().Name == "ServiceUnavailableException");
        }

        private async Task UpdateBackplaneMetrics(CancellationToken cancellationToken)
        {
            if (MetricsFactory != null)
            {
                var metrics = MetricsFactory();

                // update metrics
                await UpdateBackplaneMetricsWithLogging(metrics.Item1, metrics.Item2, cancellationToken);
            }
        }

        private Task UpdateBackplaneMetricsWithLogging(
            (string ServiceId, string Stamp) serviceInfo,
            TServiceMetrics metrics,
            CancellationToken cancellationToken)
        {
            var memoryInfo = LoggerScopeHelpers.GetProcessMemoryInfo();

            var metricsScope = new List<(string, object)>();
            metricsScope.Add((LoggerScopeHelpers.MethodScope, MethodUpdateBackplaneMetrics));
            metricsScope.Add((LoggerScopeHelpers.MemorySizeProperty, memoryInfo.memorySize));
            metricsScope.Add((LoggerScopeHelpers.TotalMemoryProperty, memoryInfo.totalMemory));
            AddMetricsScope(metricsScope, metrics);

            using (LoggerScopeHelpers.BeginScope(Logger, metricsScope.ToArray()))
            {
                Logger.LogInformation($"serviceInfo:{serviceInfo}");
            }

            return UpdateBackplaneMetrics(serviceInfo, metrics, cancellationToken);
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
