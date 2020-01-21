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
        public async Task StartAsync(IDiagnosticsLogger logger)
        {
            await logger.OperationScopeAsync(
                LogBaseName,
                (childLogger) =>
                {
                    childLogger.FluentAddValue("ContinuationStartLevel", TargetWorkerCount);

                    // Spawn other worker threads
                    for (var i = 0; i < TargetWorkerCount; i++)
                    {
                        StartWorker("InitialStart", childLogger);
                    }

                    return Task.CompletedTask;
                });

            // Trigger message level runner
            // NOTE: I don't love that this is managed in this class, but given the
            //       internal knowlege this worker needs of the WorkerPool, we have
            //       few other options.
            TaskHelper.RunBackgroundLoop(
                LogLevelBaseName,
                (childLogger) => ManageLevelAsync(childLogger),
                TimeSpan.FromSeconds(30),
                logger);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Disposed = true;
        }

        private async Task<bool> ManageLevelAsync(IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
                LogBaseName,
                (childLogger) =>
                {
                    var overCapacityCount = 0;
                    var underCapacityCount = 0;
                    var belowMinimumCount = 0;
                    var aboveMaximumCount = 0;
                    var restartedCount = 0;

                    childLogger.FluentAddValue("WorkerLevelPreCount", WorkerPool.Count);

                    // Move the pool level up and down based on how active the workers are
                    foreach (var entry in WorkerPool)
                    {
                        var worker = entry.Value;
                        if (worker.ActivityLevel < 25
                            && WorkerPool.Count > MinWorkerCount)
                        {
                            overCapacityCount++;
                            EndWorker(entry.Key, "OverCapacity", childLogger);
                        }
                        else if (worker.ActivityLevel > 175
                            && WorkerPool.Count < MaxWorkerCount)
                        {
                            underCapacityCount++;
                            StartWorker("UnderCapacity", childLogger);
                        }
                    }

                    if (WorkerPool.Count < MinWorkerCount)
                    {
                        // Make sure that we have at lease the number of levels that we want
                        for (var i = WorkerPool.Count - 1; i < TargetWorkerCount; i++)
                        {
                            belowMinimumCount++;
                            StartWorker("BelowMinimum", childLogger);
                        }
                    }
                    else if (WorkerPool.Count > MaxWorkerCount)
                    {
                        // Note, this should never happen, probably don't need to have this
                        var workers = WorkerPool.Take(MaxWorkerCount - WorkerPool.Count);
                        foreach (var worker in workers)
                        {
                            aboveMaximumCount++;
                            EndWorker(worker.Key, "AboveMaximum", childLogger);
                        }
                    }

                    // Check if any of the works have been disposed, if so remove and replace it
                    foreach (var worker in WorkerPool)
                    {
                        if (worker.Value.Disposed)
                        {
                            restartedCount++;
                            EndWorker(worker.Key, "DisposedRemove", childLogger);
                            StartWorker("DisposedAdd", childLogger);
                        }
                    }

                    childLogger.FluentAddValue("WorkerLevelPostCount", WorkerPool.Count)
                        .FluentAddValue("WorkerLevelOverCapacityCount", overCapacityCount)
                        .FluentAddValue("WorkerLevelUnderCapacityCount", underCapacityCount)
                        .FluentAddValue("WorkerLevelBelowMinimumCount", belowMinimumCount)
                        .FluentAddValue("WorkerLevelAboveMaximumCount", aboveMaximumCount)
                        .FluentAddValue("WorkerLevelRestartedCount", restartedCount);

                    return Task.FromResult(!Disposed);
                },
                (e, childLogger) => Task.FromResult(!Disposed),
                swallowException: true);
        }

        private void StartWorker(string reason, IDiagnosticsLogger logger)
        {
            _ = logger.OperationScopeAsync(
                $"{LogBaseName}_start_worker",
                (childLogger) =>
                {
                    // Create worker
                    var id = Guid.NewGuid();
                    var worker = ServiceProvider.GetService<IContinuationTaskWorker>();

                    childLogger.FluentAddBaseValue("ContinuationWorkerId", id)
                        .FluentAddValue("ContinuationWorkerStartReason", reason);

                    // Spin worker up in the backround
                    TaskHelper.RunBackgroundLoop(
                        $"{ResourceLoggingConstants.ContinuationTaskWorker}-run",
                        (taskLogger) => worker.RunAsync(taskLogger),
                        null,
                        childLogger);

                    // Add to worker store
                    WorkerPool.TryAdd(id, worker);

                    return Task.CompletedTask;
                });
        }

        private void EndWorker(Guid id, string reason, IDiagnosticsLogger logger)
        {
            _ = logger.OperationScopeAsync(
                $"{LogBaseName}_end_worker",
                (childLogger) =>
                {
                    childLogger.FluentAddBaseValue("ContinuationWorkerId", id)
                        .FluentAddValue("ContinuationWorkerEndReason", reason);

                    // Remove from worker store
                    var didRemove = WorkerPool.TryRemove(id, out var worker);

                    // Only dispose if we need to
                    if (worker != null && !worker.Disposed)
                    {
                        worker.Dispose();
                    }

                    childLogger.FluentAddBaseValue("ContinuationWorkerRemoved", didRemove);

                    return Task.CompletedTask;
                });
        }
    }
}
