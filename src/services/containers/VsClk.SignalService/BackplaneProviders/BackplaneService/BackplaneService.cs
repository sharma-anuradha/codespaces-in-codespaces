// <copyright file="BackplaneService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService;
using Microsoft.VsCloudKernel.SignalService.Common;

namespace Microsoft.VsCloudKernel.BackplaneService
{
    /// <summary>
    /// Backplane service base class.
    /// </summary>
    /// <typeparam name="TBackplaneManagerType">The backplane manager type.</typeparam>
    /// <typeparam name="TNotify">The notification type.</typeparam>
    public abstract class BackplaneService<TBackplaneManagerType, TNotify> : IHostedService
        where TBackplaneManagerType : class, IBackplaneManagerBase
    {
        private readonly CancellationTokenSource disposeTokenSource = new CancellationTokenSource();
        private readonly List<TNotify> backplaneServiceNotifications = new List<TNotify>();
        private readonly HashSet<string> registeredServices = new HashSet<string>();
        private readonly object registeredServicesLock = new object();
        private int numOfConnections;

        protected BackplaneService(
            TBackplaneManagerType backplaneManager,
            IEnumerable<TNotify> notifications,
            IServiceCounters serviceCounters,
            ILogger logger)
        {
            BackplaneManager = Requires.NotNull(backplaneManager, nameof(backplaneManager));
            this.backplaneServiceNotifications.AddRange(notifications);

            ServiceCounters = serviceCounters;
            Logger = Requires.NotNull(logger, nameof(logger));
        }

        protected CancellationToken DisposeToken => this.disposeTokenSource.Token;

        protected TBackplaneManagerType BackplaneManager { get; }

        protected IServiceCounters ServiceCounters { get; }

        protected ILogger Logger { get; }

        protected string[] RegisteredServices { get; private set; } = Array.Empty<string>();

        protected IEnumerable<TNotify> BackplaneServiceNotifications => this.backplaneServiceNotifications;

        public void AddBackplaneServiceNotification(TNotify backplaneServiceNotification)
        {
            this.backplaneServiceNotifications.Add(backplaneServiceNotification);
        }

        public void RegisterService(string serviceId)
        {
            lock (this.registeredServicesLock)
            {
                this.registeredServices.Add(serviceId);
                RegisteredServices = this.registeredServices.ToArray();
            }

            Interlocked.Increment(ref this.numOfConnections);
            Logger.LogMethodScope(LogLevel.Information, $"serviceId:{serviceId} numOfConnections:{this.numOfConnections}", nameof(RegisterService));
        }

        public void OnDisconnected(string serviceId, Exception exception)
        {
            lock (this.registeredServicesLock)
            {
                this.registeredServices.Remove(serviceId);
                RegisteredServices = this.registeredServices.ToArray();
            }

            Interlocked.Decrement(ref this.numOfConnections);
            Logger.LogMethodScope(LogLevel.Error, exception, $"OnDisconnectedAsync -> serviceId:{serviceId} numOfConnections:{this.numOfConnections}", nameof(OnDisconnected));
        }

        public void TrackMethodPerf(string methodName, TimeSpan t)
        {
            ServiceCounters?.OnInvokeMethod(GetType().Name, methodName, t);
        }

        /// <inheritdoc/>
        public abstract Task DisposeAsync();

        /// <inheritdoc/>
        public async Task RunAsync(CancellationToken stoppingToken)
        {
            const int TimespanUpdateSecs = 15;
            const int TimespanUpdateTelemetrySecs = 60;

            var updateMetricsCounter = new SecondsCounter(BackplaneManagerConst.UpdateMetricsSecs, TimespanUpdateSecs);
            var updateTelemetryCounter = new SecondsCounter(TimespanUpdateTelemetrySecs, TimespanUpdateSecs);

            ResetPerfCounters();
            while (true)
            {
                // wait
                await Task.Delay(TimeSpan.FromSeconds(TimespanUpdateSecs), stoppingToken);

                // update aggregated metrics
                if (updateMetricsCounter.Next())
                {
                    await UpdateBackplaneMetricsAsync(stoppingToken);
                }

                // update telemetry metrics
                if (updateTelemetryCounter.Next())
                {
                    LogTelemetryMetrics();
                    ResetPerfCounters();
                }
            }
        }

        protected abstract void LogTelemetryMetrics();

        protected abstract Task UpdateBackplaneMetricsAsync(CancellationToken stoppingToken);

        protected abstract void ResetPerfCounters();

        protected bool TrackDataChanged(DataChanged dataChanged, TrackDataChangedOptions options = TrackDataChangedOptions.None)
        {
            return BackplaneManager.TrackDataChanged(dataChanged, options);
        }

        protected IDisposable CreateMethodPerfTracker(string methodName)
        {
            return new MethodPerfTracker(elapsed => TrackMethodPerf(methodName, elapsed));
        }

        protected ActionBlock<(Stopwatch, T)> CreateActionBlock<T>(
            string name,
            Func<TimeSpan, T, Task> callbackItem,
            Func<T, string> changeIdResolver,
            int? maxDegreeOfParallelism,
            int? boundedCapacity)
        {
            var blockOptions = new ExecutionDataflowBlockOptions();
            if (maxDegreeOfParallelism.HasValue)
            {
                blockOptions.MaxDegreeOfParallelism = maxDegreeOfParallelism.Value;
            }

            if (boundedCapacity.HasValue)
            {
                blockOptions.BoundedCapacity = boundedCapacity.Value;
            }

            ActionBlock<(Stopwatch, T)> actionBlock = null;
            actionBlock = new ActionBlock<(Stopwatch, T)>(
                async (item) =>
                {
                    try
                    {
                        await callbackItem(item.Item1.Elapsed, item.Item2);
                        Logger.LogMethodScope(LogLevel.Debug, $"input count:{actionBlock.InputCount}", name, item.Item1.ElapsedMilliseconds);
                    }
                    catch (Exception error)
                    {
                        Logger.LogWarning(error, $"Failed to process change id:{changeIdResolver(item.Item2)} block:{name}");
                    }
                }, blockOptions);
            return actionBlock;
        }

        protected Task CompleteActionBlockAsync<T>(ActionBlock<T> actionBlock, string name)
        {
            Logger.LogDebug($"CompleteActionBlock for:{name} input count:{actionBlock.InputCount}");
            actionBlock.Complete();
            return actionBlock.Completion;
        }
    }
}
