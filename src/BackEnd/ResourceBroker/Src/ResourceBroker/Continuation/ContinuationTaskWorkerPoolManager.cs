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

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation
{
    public class ContinuationTaskWorkerPoolManager : IContinuationTaskWorkerPoolManager
    {
        private const int TargetWorkerCount = 5;
        private const int MaxWorkerCount = 10;
        private const int MinWorkerCount = 3;

        public ContinuationTaskWorkerPoolManager(
            IContinuationTaskMessagePump messagePump,
            ITaskHelper taskHelper,
            IServiceProvider serviceProvider)
        {
            MessagePump = messagePump;
            TaskHelper = taskHelper;
            ServiceProvider = serviceProvider;
            WorkerPool = new ConcurrentDictionary<string, IContinuationTaskWorker>();
        }

        private IContinuationTaskMessagePump MessagePump { get; }

        private ITaskHelper TaskHelper { get; }

        private IServiceProvider ServiceProvider { get; }

        private ConcurrentDictionary<string, IContinuationTaskWorker> WorkerPool { get; }

        private bool Disposed { get; set; }

        public Task StartAsync(IDiagnosticsLogger logger)
        {
            // Trigger message chache runner
            TaskHelper.RunBackgroundScheduleLoop(
                "continuation-task-queue-message-cache",
                TimeSpan.FromSeconds(0.5),
                (childLogger) => MessagePump.StartAsync(childLogger));

            TaskHelper.RunBackgroundSchedule(
                "continuation-task-queue-manage-level",
                TimeSpan.FromSeconds(30),
                (childLogger) => ManageLevel(childLogger));

            // Spawn other worker threads
            for (var i = 0; i < TargetWorkerCount; i++)
            {
                StartWorker(logger.FromExisting());
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Disposed = true;
        }

        private Task<bool> ManageLevel(IDiagnosticsLogger logger)
        {
            // Move the pool level up and down based on how active the workers are
            foreach (var entry in WorkerPool)
            {
                var worker = entry.Value;
                if (worker.ActivityLevel < 25
                    && WorkerPool.Count > MinWorkerCount)
                {
                    EndWorker(entry.Key, logger);
                }
                else if (worker.ActivityLevel > 175
                    && WorkerPool.Count < MaxWorkerCount)
                {
                    StartWorker(logger);
                }
            }

            if (WorkerPool.Count < TargetWorkerCount)
            {
                // Make sure that we have at lease the number of levels that we want
                for (var i = WorkerPool.Count - 1; i < TargetWorkerCount; i++)
                {
                    StartWorker(logger.FromExisting());
                }
            }
            else if (WorkerPool.Count > MaxWorkerCount)
            {
                // Note, this should never happen, probably don't need to have this
                var workers = WorkerPool.Take(MaxWorkerCount - WorkerPool.Count);
                foreach (var worker in workers)
                {
                    worker.Value.Dispose();
                }
            }

            return Task.FromResult(!Disposed);
        }

        private void StartWorker(IDiagnosticsLogger logger)
        {
            var id = Guid.NewGuid().ToString();
            var worker = ServiceProvider.GetService<IContinuationTaskWorker>();

            logger.FluentAddValue("WorkerId", id.ToString());

            TaskHelper.RunBackgroundLoop(
                "continuation-task-queue-message-job-start",
                (childLogger) => worker.Run(childLogger),
                logger);

            WorkerPool.TryAdd(id, worker);
        }

        private void EndWorker(string id, IDiagnosticsLogger logger)
        {
            if (WorkerPool.TryRemove(id, out var entry))
            {
                entry.Dispose();
            }
        }
    }

}
