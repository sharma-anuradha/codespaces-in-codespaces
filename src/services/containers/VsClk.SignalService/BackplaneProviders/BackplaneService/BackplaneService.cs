// <copyright file="BackplaneService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public class BackplaneService<TBackplaneManagerType, TNotify>
        where TBackplaneManagerType : class, IBackplaneManagerBase
    {
        private readonly CancellationTokenSource disposeTokenSource = new CancellationTokenSource();
        private readonly List<TNotify> backplaneServiceNotifications = new List<TNotify>();
        private int numOfConnections;

        protected BackplaneService(TBackplaneManagerType backplaneManager, IEnumerable<TNotify> notifications, ILogger logger)
        {
            BackplaneManager = Requires.NotNull(backplaneManager, nameof(backplaneManager));
            this.backplaneServiceNotifications.AddRange(notifications);

            Logger = Requires.NotNull(logger, nameof(logger));
        }

        protected CancellationToken DisposeToken => this.disposeTokenSource.Token;

        protected TBackplaneManagerType BackplaneManager { get; }

        protected ILogger Logger { get; }

        protected IEnumerable<TNotify> BackplaneServiceNotifications => this.backplaneServiceNotifications;

        public void AddBackplaneServiceNotification(TNotify backplaneServiceNotification)
        {
            this.backplaneServiceNotifications.Add(backplaneServiceNotification);
        }

        public void RegisterService(string serviceId)
        {
            Interlocked.Increment(ref this.numOfConnections);
            Logger.LogMethodScope(LogLevel.Information, $"serviceId:{serviceId} numOfConnections:{this.numOfConnections}", nameof(RegisterService));
        }

        public void OnDisconnected(string serviceId, Exception exception)
        {
            Interlocked.Decrement(ref this.numOfConnections);
            Logger.LogMethodScope(LogLevel.Error, exception, $"OnDisconnectedAsync -> serviceId:{serviceId} numOfConnections:{this.numOfConnections}", nameof(OnDisconnected));
        }

        protected bool TrackDataChanged(DataChanged dataChanged, TrackDataChangedOptions options = TrackDataChangedOptions.None)
        {
            return BackplaneManager.TrackDataChanged(dataChanged, options);
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
