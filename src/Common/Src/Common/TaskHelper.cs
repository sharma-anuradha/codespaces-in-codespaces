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
        public void RunBackgroundLoop(
            string name,
            Func<IDiagnosticsLogger, Task<bool>> callback,
            TimeSpan? schedule = null,
            IDiagnosticsLogger logger = null,
            bool autoLogLoopOperation = false,
            Func<Exception, bool> errLoopCallback = default)
        {
            logger = (logger ?? Logger)
                .FluentAddBaseValue("TaskWorkerRunId", Guid.NewGuid())
                .FluentAddBaseValue("TaskRunTarget", name);

            RunBackground(
                "task_helper_run_background_loop",
                async (childLogger) =>
                {
                    logger.LogInfo("task_helper_run_background_loop_started");

                    while (await RunBackgroundLoopCore(
                        name, callback, childLogger, autoLogLoopOperation, errLoopCallback))
                    {
                        if (schedule != null)
                        {
                            await Task.Delay(schedule.Value);
                        }
                    }
                },
                logger);
        }

        /// <inheritdoc/>
        public void RunBackground(
            string name,
            Func<IDiagnosticsLogger, Task> callback,
            IDiagnosticsLogger logger = null,
            bool autoLogOperation = true,
            Action<Exception> errCallback = default,
            TimeSpan? delay = null)
        {
            if (delay == null)
            {
                Task.Run(RunBackgroundCore(name, callback, logger, autoLogOperation, errCallback));
            }
            else
            {
                Task.Delay((int)delay.Value.TotalMilliseconds)
                    .ContinueWith(x => RunBackground(name, callback, logger, autoLogOperation, errCallback));
            }
        }

        /// <inheritdoc/>
        public void RunBackgroundLong(
            string name,
            Func<IDiagnosticsLogger, Task> callback,
            IDiagnosticsLogger logger = null,
            bool autoLogOperation = true,
            Action<Exception> errCallback = default,
            TimeSpan? delay = null)
        {
            if (delay == null)
            {
                Task.Factory.StartNew(RunBackgroundCore(name, callback, logger, autoLogOperation, errCallback), TaskCreationOptions.LongRunning);
            }
            else
            {
                Task.Delay((int)delay.Value.TotalMilliseconds)
                    .ContinueWith(x => RunBackgroundLong(name, callback, logger, autoLogOperation, errCallback));
            }
        }

        /// <inheritdoc/>
        public void RunBackgroundEnumerable<T>(
           string name,
           IEnumerable<T> list,
           Func<T, IDiagnosticsLogger, Task> callback,
           IDiagnosticsLogger logger = null,
           Func<T, IDiagnosticsLogger, Task<IDisposable>> obtainLease = null,
           Action<T, Exception> errItemCallback = default,
           int concurrentLimit = 3,
           int successDelay = 250,
           int failDelay = 100)
        {
            // Trigger to run things in the background
            RunBackground(
                "task_helper_run_background_enumerable",
                (childLogger) => RunBackgroundEnumerableAsync(
                    name, list, callback, childLogger, obtainLease, errItemCallback, concurrentLimit, successDelay, failDelay),
                logger,
                autoLogOperation: false);
        }

        /// <inheritdoc/>
        public Task RunBackgroundEnumerableAsync<T>(
            string name,
            IEnumerable<T> list,
            Func<T, IDiagnosticsLogger, Task> callback,
            IDiagnosticsLogger logger = null,
            Func<T, IDiagnosticsLogger, Task<IDisposable>> obtainLease = null,
            Action<T, Exception> errItemCallback = default,
            int concurrentLimit = 3,
            int successDelay = 250,
            int failDelay = 100)
        {
            logger = logger ?? Logger;

            // When debugging, just run one at a time
            concurrentLimit = Math.Max(concurrentLimit, 1);
            concurrentLimit = Debugger.IsAttached ? 1 : concurrentLimit;

            // Log the main task
            return logger.OperationScopeAsync(
                "task_helper_run_background_enumerable",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue("TaskWorkerRunId", Guid.NewGuid())
                        .FluentAddBaseValue("TaskRunTarget", name);

                    // Trigger core runner
                    await RunBackgroundEnumerableCore(
                        name, list, callback, childLogger, obtainLease, errItemCallback, concurrentLimit, successDelay, failDelay);
                });
        }

        /// <inheritdoc/>
        public async Task<bool> RetryUntilSuccessOrTimeout(
            string name,
            Func<Task<bool>> callback,
            TimeSpan timeoutTimeSpan,
            TimeSpan? waitTimeSpan = null,
            IDiagnosticsLogger logger = null,
            Action onTimeout = null)
        {
            logger = Logger ?? logger;

            // Log the main task
            return await logger.OperationScopeAsync(
                "task_helper_run_until_success_or_timeout",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue("TaskRunId", Guid.NewGuid())
                        .FluentAddBaseValue("TaskRunName", name)
                        .FluentAddValue("TaskTimeoutTimeSpan", timeoutTimeSpan);

                    var timer = Stopwatch.StartNew();
                    var success = false;
                    var tryCount = 0;

                    // Track a log statement per try
                    do
                    {
                        // Log each attempt
                        await childLogger.OperationScopeAsync(
                            "task_helper_run_until_success_or_timeout_item",
                            async (tryLogger) =>
                            {
                                tryLogger.FluentAddValue("IterateTryCount", tryCount)
                                    .FluentAddValue("TaskTimeoutTimeSpan", timeoutTimeSpan);

                                // Execute core
                                success = await callback();

                                tryLogger.FluentAddValue("IterateSuccess", success)
                                    .FluentAddDuration("IterateTotalRun", timer);
                            });

                        // Add delay between tries
                        if (!success && waitTimeSpan.HasValue)
                        {
                            await Task.Delay((int)waitTimeSpan.Value.TotalMilliseconds);
                        }

                        tryCount++;
                    }
                    while (!success && timer.ElapsedMilliseconds < timeoutTimeSpan.TotalMilliseconds);

                    // If things weren't successful
                    if (!success && onTimeout != null)
                    {
                        onTimeout();
                    }

                    logger.FluentAddValue("TaskTotalTryCount", tryCount)
                        .FluentAddValue("TaskSuccess", success);

                    return success;
                });
        }

        private Task<bool> RunBackgroundLoopCore(
            string name,
            Func<IDiagnosticsLogger, Task<bool>> callback,
            IDiagnosticsLogger logger,
            bool autoLogOperation,
            Func<Exception, bool> errItemCallback)
        {
            if (autoLogOperation)
            {
                return logger.OperationScopeAsync(
                    name, callback, (e) => errItemCallback != null ? errItemCallback(e) : true, swallowException: true);
            }
            else
            {
                // In this case we only want to catch unhandled exceptions
                try
                {
                    // Trigger main callback with a new logger
                    return callback(logger.NewChildLogger());
                }
                catch (Exception e)
                {
                    // Log unhandled exception
                    logger.LogException("task_run_error", e);

                    return Task.FromResult(errItemCallback != null ? errItemCallback(e) : true);
                }
            }
        }

        private Func<Task> RunBackgroundCore(
            string name,
            Func<IDiagnosticsLogger, Task> callback,
            IDiagnosticsLogger logger,
            bool autoLogOperation,
            Action<Exception> errCallback)
        {
            logger = (logger ?? Logger)
                .FluentAddBaseValue("TaskRunId", Guid.NewGuid())
                .FluentAddBaseValue("TaskRunName", name);

            if (autoLogOperation)
            {
                return () => logger.OperationScopeAsync(name, callback, errCallback, swallowException: true);
            }
            else
            {
                // In this case make sure we catch unhandled exceptions
                return () =>
                {
                    try
                    {
                        // Trigger main callback with a new logger
                        return callback(logger.NewChildLogger());
                    }
                    catch (Exception e)
                    {
                        // Log unhandled exception
                        logger.LogException("task_run_error", e);

                        // Run error callback if needed
                        if (errCallback != null)
                        {
                            errCallback(e);
                        }

                        return Task.CompletedTask;
                    }
                };
            }
        }

        private async Task RunBackgroundEnumerableCore<T>(
           string name,
           IEnumerable<T> list,
           Func<T, IDiagnosticsLogger, Task> callback,
           IDiagnosticsLogger logger,
           Func<T, IDiagnosticsLogger, Task<IDisposable>> obtainLease,
           Action<T, Exception> errItemCallback,
           int concurrentLimit,
           int successDelay,
           int failDelay)
        {
            var results = new List<TaskCompletionSource<Exception>>();
            var localLock = new object();
            var concurrentCount = 0;
            var index = 0;

            logger.FluentAddValue("IterateItemCount", list.Count());

            // Run through each item in the list
            foreach (var item in list)
            {
                var tryCount = 0;
                var lockAchieved = false;

                // Task tracking
                var localCompletion = new TaskCompletionSource<Exception>();
                results.Add(localCompletion);

                // Track a log statement per item
                await logger.OperationScopeAsync(
                    "task_helper_run_background_enumerable_item",
                    async (itemLogger) =>
                    {
                        itemLogger.FluentAddBaseValue("IterateItemRunId", Guid.NewGuid())
                            .FluentAddValue("IterateItemCount", list.Count())
                            .FluentAddValue("IterateItemIndex", index)
                            .FluentAddValue("LockConcurrentLimit", concurrentLimit)
                            .FluentAddValue("LockConcurrentPreCount", concurrentCount);

                        // Continue trying to obtain lock till we do
                        do
                        {
                            // Track a log statement per try
                            await itemLogger.OperationScopeAsync(
                                "task_helper_run_background_enumerable_item_try",
                                async (itemTryLogger) =>
                                {
                                    itemTryLogger.FluentAddBaseValue("IterateItemRunTryId", Guid.NewGuid())
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

                                    itemTryLogger.FluentAddValue("LockAchived", lockAchieved)
                                        .FluentAddValue("LockConcurrentPostCount", concurrentCount);

                                    // If we got the lock run the background task
                                    if (lockAchieved)
                                    {
                                        // Spawn work to take place in the background, this allows for the
                                        // concurrent worker limit to be achived, otherwise we would only
                                        // be running one at time.
                                        RunBackground(
                                            name,
                                            async (executeLogger) =>
                                                {
                                                    // Core task execution
                                                    var didExecute = await RunBackgroundEnumerableItemCore(
                                                        item, callback, executeLogger, obtainLease);

                                                    // Pause to give some time between runs (mainly to give other
                                                    // workers on other machines a case to work through things)
                                                    if (didExecute && successDelay > 0)
                                                    {
                                                        var delayDifference = (int)(successDelay * 0.1);
                                                        await Task.Delay(Random.Next(successDelay - delayDifference, successDelay + delayDifference));
                                                    }

                                                    // Track completion
                                                    localCompletion.SetResult(null);
                                                },
                                            itemTryLogger,
                                            errCallback: (e) =>
                                                {
                                                    // Track completion
                                                    localCompletion.SetResult(e);

                                                    // Execute users callback if needed
                                                    if (errItemCallback != null)
                                                    {
                                                        errItemCallback(item, e);
                                                    }
                                                });

                                        // Make sure we reduce the count, even if there was an error
                                        concurrentCount--;
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

                        itemLogger.FluentAddValue("LockTryCount", tryCount)
                            .FluentAddValue("LockConcurrentPostCount", concurrentCount);
                    });

                index++;
            }

            // Exception handling
            var exceptions = (await Task.WhenAll(results.Select(x => x.Task)))
                .Where(x => x != null);

            logger.FluentAddValue("IterateExceptionCount", exceptions.Count());

            if (exceptions.Any())
            {
                throw new AggregateException("Run Background Enumerable items threw excpetions", exceptions);
            }
        }

        private async Task<bool> RunBackgroundEnumerableItemCore<T>(
            T item,
            Func<T, IDiagnosticsLogger, Task> callback,
            IDiagnosticsLogger logger,
            Func<T, IDiagnosticsLogger, Task<IDisposable>> obtainLease)
        {
            var success = false;

            logger.FluentAddValue("LeaseShouldObtain", obtainLease != null);

            // If we don't have lease, normal execute
            if (obtainLease == null)
            {
                await callback(item, logger.NewChildLogger());

                success = true;
            }
            else
            {
                // Obtain lease as needed
                using (var lease = await obtainLease(item, logger.NewChildLogger()))
                {
                    logger.FluentAddValue("LeaseNotFound", lease == null);

                    // Obnly execute if we have somethin g to do
                    if (lease != null)
                    {
                        await callback(item, logger.NewChildLogger());

                        success = true;
                    }
                }
            }

            return success;
        }
    }
}