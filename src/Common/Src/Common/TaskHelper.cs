// <copyright file="TaskHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Task helper library.
    /// </summary>
    public class TaskHelper : ITaskHelper
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TaskHelper"/> class.
        /// </summary>
        /// <param name="logger">Target default logger.</param>
        public TaskHelper(
            IDiagnosticsLogger logger)
        {
            Random = new Random();
            Logger = logger;
        }

        private Random Random { get; }

        private IDiagnosticsLogger Logger { get; }

        /// <inheritdoc/>
        public void RunBackgroundLoop(string name, Func<IDiagnosticsLogger, Task<bool>> callback, TimeSpan? schedule = null, IDiagnosticsLogger logger = null)
        {
            logger = (logger ?? Logger).WithValues(new LogValueSet())
                .FluentAddBaseValue("TaskManagerId", Guid.NewGuid().ToString())
                .FluentAddBaseValue("TaskName", name);

            var wrappedCallback = WrapCallback(name, callback, logger);

            logger.LogInfo("task_helper_run_background_loop_started");

            Task.Run(
                async () =>
                {
                    while (await wrappedCallback())
                    {
                        if (schedule != null)
                        {
                            await Task.Delay(schedule.Value);
                        }
                    }

                    logger.LogInfo("task_helper_run_background_loop_exited");
                });
        }

        /// <inheritdoc/>
        public void RunBackground(string name, Func<IDiagnosticsLogger, Task> callback, IDiagnosticsLogger logger = null, TimeSpan? delay = null)
        {
            if (delay == null)
            {
                Task.Run(WrapCallback(name, callback, logger));
            }
            else
            {
                Task.Delay((int)delay.Value.TotalMilliseconds).ContinueWith(x => RunBackground(name, callback, logger));
            }
        }

        /// <inheritdoc/>
        public void RunBackgroundLong(string name, Func<IDiagnosticsLogger, Task> callback, IDiagnosticsLogger logger = null, TimeSpan? delay = null)
        {
            if (delay == null)
            {
                Task.Factory.StartNew(WrapCallback(name, callback, logger), TaskCreationOptions.LongRunning);
            }
            else
            {
                Task.Delay((int)delay.Value.TotalMilliseconds).ContinueWith(x => RunBackgroundLong(name, callback, logger));
            }
        }

        /// <inheritdoc/>
        public void RunBackgroundEnumerable<T>(
            string name,
            IEnumerable<T> list,
            Func<T, IDiagnosticsLogger, Task> callback,
            IDiagnosticsLogger logger = null,
            Func<T, Task<IDisposable>> obtainLease = null,
            int concurrentLimit = 3,
            int successDelay = 250,
            int failDelay = 100)
        {
            logger.FluentAddBaseValue("IterateRunId", Guid.NewGuid())
                .FluentAddBaseValue("IterateRunName", name);

            RunBackground(
                "task_helper_run_background_enumerable_started",
                (childLogger) => RunBackgroundEnumerableAsync(
                    name, list, callback, childLogger, obtainLease, concurrentLimit, successDelay, failDelay),
                logger);
        }

        /// <inheritdoc/>
        public Task RunBackgroundEnumerableAsync<T>(
            string name,
            IEnumerable<T> list,
            Func<T, IDiagnosticsLogger, Task> callback,
            IDiagnosticsLogger logger = null,
            Func<T, Task<IDisposable>> obtainLease = null,
            int concurrentLimit = 3,
            int successDelay = 250,
            int failDelay = 100)
        {
            logger = logger ?? Logger;

            logger.FluentAddBaseValue("IterateRunId", Guid.NewGuid())
                .FluentAddBaseValue("IterateRunName", name);

            return logger.OperationScopeAsync(
                "task_helper_run_background_enumerable_started",
                async () => await RunBackgroundEnumerableCore(
                    name, list, callback, logger, obtainLease, concurrentLimit, successDelay, failDelay),
                swallowException: true);
        }

        /// <inheritdoc/>
        public async Task<bool> RetryUntilSuccessOrTimeout(Func<Task<bool>> task, TimeSpan waitTimeSpan, TimeSpan timeoutTimeSpan, Action onTimeout = null)
        {
            var timer = Stopwatch.StartNew();
            var success = false;
            while (!success && timer.ElapsedMilliseconds < timeoutTimeSpan.TotalMilliseconds)
            {
                success = await task();

                if (!success)
                {
                    await Task.Delay((int)waitTimeSpan.TotalMilliseconds);
                }
            }

            if (!success && onTimeout != null)
            {
                onTimeout();
            }

            return success;
        }

        private Func<Task> WrapCallback(string name, Func<IDiagnosticsLogger, Task> callback, IDiagnosticsLogger logger = null)
        {
            return async () =>
            {
                logger = logger ?? Logger;
                try
                {
                    await callback(logger.WithValues(new LogValueSet()));
                }
                catch (Exception e)
                {
                    logger.LogException("task_helper_error", e);
                }
            };
        }

        private Func<Task<bool>> WrapCallback(string name, Func<IDiagnosticsLogger, Task<bool>> callback, IDiagnosticsLogger logger = null)
        {
            return async () =>
            {
                var result = true;
                logger = logger ?? Logger;

                try
                {
                    result = await callback(logger.WithValues(new LogValueSet()));
                }
                catch (Exception e)
                {
                    logger.LogException("task_run_error", e);
                }

                return result;
            };
        }

        private async Task RunBackgroundEnumerableCore<T>(
            string name,
            IEnumerable<T> list,
            Func<T, IDiagnosticsLogger, Task> callback,
            IDiagnosticsLogger logger,
            Func<T, Task<IDisposable>> obtainLease,
            int concurrentLimit,
            int successDelay,
            int failDelay)
        {
            var localLock = new object();
            var concurrentCount = 0;
            var index = 0;

            logger.FluentAddValue("IterateItemCount", list.Count());

            // Run through each item in the list
            foreach (var item in list)
            {
                var tryCount = 0;
                var lockAchieved = false;

                // Track a log statement per item
                var outterLoopLogger = logger.WithValues(new LogValueSet());
                await outterLoopLogger.OperationScopeAsync(
                    "task_helper_run_background_enumerable_item_started",
                    async () =>
                    {
                        outterLoopLogger.FluentAddBaseValue("IterateItemRunId", Guid.NewGuid())
                            .FluentAddValue("IterateItemCount", list.Count())
                            .FluentAddValue("IterateItemIndex", index)
                            .FluentAddValue("LockConcurrentLimit", concurrentLimit)
                            .FluentAddValue("LockConcurrentPreCount", concurrentCount);

                        // Continue trying to obtain lock till we do
                        do
                        {
                            // Track a log statement per try
                            var innerLoopLogger = outterLoopLogger.WithValues(new LogValueSet());
                            await innerLoopLogger.OperationScopeAsync(
                                "task_helper_run_background_enumerable_item_try_started",
                                async () =>
                                {
                                    innerLoopLogger.FluentAddBaseValue("IterateItemRunTryId", Guid.NewGuid())
                                        .FluentAddValue("IterateItemCount", list.Count())
                                        .FluentAddValue("IterateItemIndex", index)
                                        .FluentAddValue("LockConcurrentLimit", concurrentLimit)
                                        .FluentAddValue("LockConcurrentPreCount", concurrentCount)
                                        .FluentAddValue("LockTryCount", tryCount);

                                    // Try and get lock to do the work
                                    if (concurrentCount < concurrentLimit)
                                    {
                                        lock (localLock)
                                        {
                                            if (concurrentCount < concurrentLimit)
                                            {
                                                lockAchieved = true;
                                                concurrentCount++;
                                            }
                                        }
                                    }

                                    innerLoopLogger.FluentAddValue("LockAchived", lockAchieved)
                                        .FluentAddValue("LockConcurrentPostCount", concurrentCount);

                                    // If we got the lock run the backgroun task
                                    if (lockAchieved)
                                    {
                                        // Spawn work to take place in the background, this allows for the 
                                        // concurrent worker limit to be achived, otherwise we would only
                                        // be running one at time. 
                                        RunBackground(
                                            name,
                                            async (executeLogger) =>
                                            {
                                                await executeLogger.OperationScopeAsync(
                                                    name,
                                                    async () =>
                                                    {
                                                        // Core task execution
                                                        var didExecute = await RunBackgroundEnumerableCoreItem(
                                                            name, item, callback, executeLogger, obtainLease, successDelay);

                                                        // Pause to give some time between runs (mainly to give other
                                                        // workers on other machines a case to work through things)
                                                        if (didExecute && successDelay > 0)
                                                        {
                                                            var delayDifference = (int)(successDelay * 0.1);
                                                            await Task.Delay(Random.Next(successDelay - delayDifference, successDelay + delayDifference));
                                                        }
                                                    },
                                                    swallowException: true);

                                                // Make sure we reduce the count, even if there was an error
                                                concurrentCount--;
                                            },
                                            innerLoopLogger);
                                    }
                                    else
                                    {
                                        // Pause to give some time between runs (mainly to give other running
                                        // tasks a chance to finish)
                                        if (failDelay > 0)
                                        {
                                            var delayDifference = (int)(failDelay * 0.1);
                                            await Task.Delay(Random.Next(failDelay - delayDifference, failDelay + delayDifference));
                                        }
                                    }
                                });

                            tryCount++;
                        }
                        while (!lockAchieved);

                        outterLoopLogger.FluentAddValue("IterateItemSuccess", lockAchieved)
                            .FluentAddValue("LockTryCount", tryCount)
                            .FluentAddValue("LockConcurrentPostCount", concurrentCount);
                    },
                    swallowException: true);

                index++;
            }
        }

        private async Task<bool> RunBackgroundEnumerableCoreItem<T>(
            string name,
            T item,
            Func<T, IDiagnosticsLogger, Task> callback,
            IDiagnosticsLogger logger,
            Func<T, Task<IDisposable>> obtainLease,
            int successDelay)
        {
            var success = false;

            // If we don't have lease, normal execute
            if (obtainLease == null)
            {
                await callback(item, logger.WithValues(new LogValueSet()));

                success = true;
            }
            else
            {
                // Obtain lease as needed
                using (var lease = await obtainLease(item))
                {
                    // Obnly execute if we have something to do
                    if (lease != null)
                    {
                        await callback(item, logger.WithValues(new LogValueSet()));

                        success = true;
                    }
                }
            }

            return success;
        }
    }
}