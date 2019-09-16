// <copyright file="ContinuationTaskWorkerPoolManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation
{
    /// <summary>
    /// Continuation manager that controls the workers that gets available messages
    /// and passes them off to the activator for processing.
    /// </summary>
    public class ContinuationTaskWorkerPoolManager : IContinuationTaskWorkerPoolManager
    {
        private const string LogBaseName = ResourceLoggingConstants.ContinuationTaskWorkerPoolManager;
        private const string LogLevelBaseName = ResourceLoggingConstants.ContinuationTaskWorkerPoolManager + "-manage-level";
        private const int TargetWorkerCount = 5;
        private const int MaxWorkerCount = 10;
        private const int MinWorkerCount = 3;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContinuationTaskWorkerPoolManager"/> class.
        /// </summary>
        /// <param name="taskHelper">Target task helper.</param>
        /// <param name="serviceProvider">Target service provider.</param>
        public ContinuationTaskWorkerPoolManager(
            ITaskHelper taskHelper,
            IServiceProvider serviceProvider)
        {
            TaskHelper = taskHelper;
            ServiceProvider = serviceProvider;
            WorkerPool = new ConcurrentDictionary<Guid, IContinuationTaskWorker>();
        }

        /// <inheritdoc/>
        public int CurrentWorkerCount
        {
            get { return WorkerPool.Count; }
        }

        private ITaskHelper TaskHelper { get; }

        private IServiceProvider ServiceProvider { get; }

        private ConcurrentDictionary<Guid, IContinuationTaskWorker> WorkerPool { get; }

        private bool Disposed { get; set; }

        /// <inheritdoc/>
        public Task StartAsync(IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(LogBaseName, () => InnerStartAsync(logger), swallowException: true);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Disposed = true;
        }

        private Task InnerStartAsync(IDiagnosticsLogger logger)
        {
            logger.FluentAddValue("ContinuationStartLevel", TargetWorkerCount);

            // Spawn other worker threads
            for (var i = 0; i < TargetWorkerCount; i++)
            {
                StartWorker("InitialStart", logger.WithValues(new LogValueSet()));
            }

            // Trigger message level runner
            TaskHelper.RunBackgroundLoop(
                LogLevelBaseName,
                (childLogger) => ManageLevelAsync(childLogger),
                TimeSpan.FromSeconds(30));

            return Task.CompletedTask;
        }

        private Task<bool> ManageLevelAsync(IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(LogLevelBaseName, () => InnerManageLevelAsync(logger), (e) => true, swallowException: true);
        }

        private Task<bool> InnerManageLevelAsync(IDiagnosticsLogger logger)
        {
            var overCapacityCount = 0;
            var underCapacityCount = 0;
            var belowMinimumCount = 0;
            var aboveMaximumCount = 0;
            var restartedCount = 0;

            logger.FluentAddValue("WorkerLevelPreCount", WorkerPool.Count);

            // Move the pool level up and down based on how active the workers are
            foreach (var entry in WorkerPool)
            {
                var worker = entry.Value;
                if (worker.ActivityLevel < 25
                    && WorkerPool.Count > MinWorkerCount)
                {
                    overCapacityCount++;
                    EndWorker(entry.Key, "OverCapacity", logger.WithValues(new LogValueSet()));
                }
                else if (worker.ActivityLevel > 175
                    && WorkerPool.Count < MaxWorkerCount)
                {
                    underCapacityCount++;
                    StartWorker("UnderCapacity", logger.WithValues(new LogValueSet()));
                }
            }

            if (WorkerPool.Count < MinWorkerCount)
            {
                // Make sure that we have at lease the number of levels that we want
                for (var i = WorkerPool.Count - 1; i < TargetWorkerCount; i++)
                {
                    belowMinimumCount++;
                    StartWorker("BelowMinimum", logger.WithValues(new LogValueSet()));
                }
            }
            else if (WorkerPool.Count > MaxWorkerCount)
            {
                // Note, this should never happen, probably don't need to have this
                var workers = WorkerPool.Take(MaxWorkerCount - WorkerPool.Count);
                foreach (var worker in workers)
                {
                    aboveMaximumCount++;
                    EndWorker(worker.Value, "AboveMaximum", logger.WithValues(new LogValueSet()));
                }
            }

            // Check if any of the works have been disposed, if so remove and replace it
            foreach (var worker in WorkerPool)
            {
                if (worker.Value.Disposed)
                {
                    restartedCount++;
                    EndWorker(worker.Value, "DisposedRemove", logger.WithValues(new LogValueSet()));
                    StartWorker("DisposedAdd", logger.WithValues(new LogValueSet()));
                }
            }

            logger.FluentAddValue("WorkerLevelPostCount", WorkerPool.Count)
                .FluentAddValue("WorkerLevelOverCapacityCount", overCapacityCount)
                .FluentAddValue("WorkerLevelUnderCapacityCount", underCapacityCount)
                .FluentAddValue("WorkerLevelBelowMinimumCount", belowMinimumCount)
                .FluentAddValue("WorkerLevelAboveMaximumCount", aboveMaximumCount)
                .FluentAddValue("WorkerLevelRestartedCount", restartedCount);

            return Task.FromResult(!Disposed);
        }

        private void StartWorker(string reason, IDiagnosticsLogger logger)
        {
            var id = Guid.NewGuid();
            var worker = ServiceProvider.GetService<IContinuationTaskWorker>();

            logger.FluentAddBaseValue("ContinuationWorkerId", id)
                .FluentAddValue("ContinuationWorkerStartReason", reason);

            TaskHelper.RunBackgroundLoop(
                $"{ResourceLoggingConstants.ContinuationTaskWorker}-run",
                async (childLogger) => await worker.RunAsync(childLogger),
                null,
                logger);

            WorkerPool.TryAdd(id, worker);

            logger.LogInfo($"{LogBaseName}_start_worker");
        }

        private void EndWorker(Guid id, string reason, IDiagnosticsLogger logger)
        {
            if (WorkerPool.TryRemove(id, out var worker))
            {
                EndWorker(worker, reason, logger);
            }
        }

        private void EndWorker(IContinuationTaskWorker worker, string reason, IDiagnosticsLogger logger)
        {
            worker.Dispose();

            logger.FluentAddValue("ContinuationWorkerEndReason", reason)
                .LogInfo($"{LogBaseName}_end_worker");
        }
    }
}
