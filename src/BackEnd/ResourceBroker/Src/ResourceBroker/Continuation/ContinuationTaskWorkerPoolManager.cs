// <copyright file="ContinuationTaskQueueManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation
{
    /// <summary>
    /// 
    /// </summary>
    public class ContinuationTaskWorkerPoolManager : IContinuationTaskWorkerPoolManager
    {
        private const string LogBaseName = ResourceLoggingsConstants.ContinuationTaskWorkerPoolManager;
        private const int TargetWorkerCount = 5;
        private const int MaxWorkerCount = 10;
        private const int MinWorkerCount = 3;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContinuationTaskWorkerPoolManager"/> class.
        /// </summary>
        /// <param name="taskHelper"></param>
        /// <param name="serviceProvider"></param>
        public ContinuationTaskWorkerPoolManager(
            ITaskHelper taskHelper,
            IServiceProvider serviceProvider)
        {
            TaskHelper = taskHelper;
            ServiceProvider = serviceProvider;
            WorkerPool = new ConcurrentDictionary<string, IContinuationTaskWorker>();
        }

        /// <inheritdoc/>
        public int CurrentWorkerCount
        {
            get { return WorkerPool.Count; }
        }

        private ITaskHelper TaskHelper { get; }

        private IServiceProvider ServiceProvider { get; }

        private ConcurrentDictionary<string, IContinuationTaskWorker> WorkerPool { get; }

        private bool Disposed { get; set; }

        /// <inheritdoc/>
        public Task StartAsync(IDiagnosticsLogger logger)
        {
            // Spawn other worker threads
            for (var i = 0; i < TargetWorkerCount; i++)
            {
                StartWorker("InitialStart", logger.FromExisting());
            }

            // Trigger message level runner
            TaskHelper.RunBackgroundLoop(
                $"{LogBaseName}-manage-level",
                (childLogger) => ManageLevelAsync(childLogger),
                TimeSpan.FromSeconds(30));

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Disposed = true;
        }

        private Task<bool> ManageLevelAsync(IDiagnosticsLogger logger)
        {
            var overCapacityCount = 0;
            var underCapacityCount = 0;
            var belowMinimumCount = 0;
            var aboveMaximumCount = 0;

            logger.FluentAddValue("ContinuationPreLevelCount", WorkerPool.Count.ToString());

            // Move the pool level up and down based on how active the workers are
            foreach (var entry in WorkerPool)
            {
                var worker = entry.Value;
                if (worker.ActivityLevel < 25
                    && WorkerPool.Count > MinWorkerCount)
                {
                    overCapacityCount++;
                    EndWorker(entry.Key, "OverCapacity", logger.FromExisting());
                }
                else if (worker.ActivityLevel > 175
                    && WorkerPool.Count < MaxWorkerCount)
                {
                    underCapacityCount++;
                    StartWorker("UnderCapacity", logger.FromExisting());
                }
            }

            if (WorkerPool.Count < MinWorkerCount)
            {
                // Make sure that we have at lease the number of levels that we want
                for (var i = WorkerPool.Count - 1; i < TargetWorkerCount; i++)
                {
                    belowMinimumCount++;
                    StartWorker("BelowMinimum", logger.FromExisting());
                }
            }
            else if (WorkerPool.Count > MaxWorkerCount)
            {
                // Note, this should never happen, probably don't need to have this
                var workers = WorkerPool.Take(MaxWorkerCount - WorkerPool.Count);
                foreach (var worker in workers)
                {
                    aboveMaximumCount++;
                    EndWorker(worker.Value, "AboveMaximum", logger.FromExisting());
                }
            }

            logger.FluentAddValue("ContinuationPostLevelCount", WorkerPool.Count.ToString())
                .FluentAddValue("ContinuationOverCapacityCount", overCapacityCount.ToString())
                .FluentAddValue("ContinuationUnderCapacityCount", underCapacityCount.ToString())
                .FluentAddValue("ContinuationBelowMinimumCount", belowMinimumCount.ToString())
                .FluentAddValue("ContinuationAboveMaximumCount", aboveMaximumCount.ToString());

            return Task.FromResult(!Disposed);
        }

        private void StartWorker(string reason, IDiagnosticsLogger logger)
        {
            var id = Guid.NewGuid().ToString();
            var worker = ServiceProvider.GetService<IContinuationTaskWorker>();

            logger.FluentAddBaseValue("ContinuationWorkerId", id.ToString())
                .FluentAddValue("ContinuatioWorkerStartReason", reason);

            TaskHelper.RunBackgroundLoop(
                $"{ResourceLoggingsConstants.ContinuationTaskWorker}-run",
                async (childLogger) => await worker.RunAsync(childLogger),
                null,
                logger);

            WorkerPool.TryAdd(id, worker);

            logger.LogInfo($"{LogBaseName}-start-worker");
        }

        private void EndWorker(string id, string reason, IDiagnosticsLogger logger)
        {
            if (WorkerPool.TryRemove(id, out var worker))
            {
                EndWorker(worker, reason, logger);
            }
        }

        private void EndWorker(IContinuationTaskWorker worker, string reason, IDiagnosticsLogger logger)
        {
            worker.Dispose();

            logger.LogInfo($"{LogBaseName}-end-worker");
        }
    }

}
