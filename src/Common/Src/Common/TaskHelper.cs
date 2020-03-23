// <copyright file="TaskHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
            Func<Exception, IDiagnosticsLogger, bool> errLoopCallback = default)
        {
            Task.Run(() => RunBackgroundLoopAsync(
                name, callback, schedule, logger, autoLogLoopOperation, errLoopCallback));
        }

        /// <inheritdoc/>
        public async Task RunBackgroundLoopAsync(
            string name,
            Func<IDiagnosticsLogger, Task<bool>> callback,
            TimeSpan? schedule = null,
            IDiagnosticsLogger logger = null,
            bool autoLogLoopOperation = false,
            Func<Exception, IDiagnosticsLogger, bool> errLoopCallback = default)
        {
            logger = (logger ?? Logger.NewChildLogger())
                .FluentAddBaseValue("TaskWorkerLoopRunId", Guid.NewGuid())
                .FluentAddBaseValue("TaskLoopRunName", name)
                .FluentAddBaseValue("TaskRunName", name);

            await RunBackgroundLoopItemCore(
                name, callback, logger, autoLogLoopOperation, errLoopCallback);

            RunBackground(
                "task_helper_run_background_loop",
                async (childLogger) =>
                {
                    logger.LogInfo("task_helper_run_background_loop_started");

                    do
                    {
                        if (schedule != null)
                        {
                            await Task.Delay(schedule.Value);
                        }
                    }
                    while (await RunBackgroundLoopItemCore(
                        name, callback, childLogger, autoLogLoopOperation, errLoopCallback));
                },
                logger);
        }

        /// <inheritdoc/>
        public void RunBackground(
            string name,
            Func<IDiagnosticsLogger, Task> callback,
            IDiagnosticsLogger logger = null,
            bool autoLogOperation = true,
            Func<Exception, IDiagnosticsLogger, Task> errCallback = default,
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
            Func<Exception, IDiagnosticsLogger, Task> errCallback = default,
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
           int itemDelay = 250)
        {
            // Trigger to run things in the background
            RunBackground(
                "task_helper_run_background_enumerable",
                (childLogger) => RunEnumerableAsync(
                    name, list, callback, childLogger, obtainLease, itemDelay),
                logger,
                autoLogOperation: false);
        }

        /// <inheritdoc/>
        public Task RunEnumerableAsync<T>(
            string name,
            IEnumerable<T> list,
            Func<T, IDiagnosticsLogger, Task> callback,
            IDiagnosticsLogger logger = null,
            Func<T, IDiagnosticsLogger, Task<IDisposable>> obtainLease = null,
            int itemDelay = 250)
        {
            logger = logger ?? Logger.NewChildLogger();

            // Log the main task
            return logger.OperationScopeAsync(
                "task_helper_run_background_enumerable",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue("TaskWorkerEnumerableRunId", Guid.NewGuid())
                        .FluentAddBaseValue("TaskEnumerableRunName", name)
                        .FluentAddBaseValue("TasksRunName", name);

                    // Trigger core runner
                    await RunEnumerableCoreAsync(
                        name, list, callback, childLogger, obtainLease, itemDelay);
                });
        }

        /// <inheritdoc/>
        public void RunBackgroundConcurrentEnumerable<T>(
           string name,
           IEnumerable<T> list,
           Func<T, IDiagnosticsLogger, Task> callback,
           IDiagnosticsLogger logger = null,
           Func<T, IDiagnosticsLogger, Task<IDisposable>> obtainLease = null,
           Action<T, Exception, IDiagnosticsLogger> errItemCallback = default,
           int concurrentLimit = 3,
           int successDelay = 250)
        {
            // Trigger to run things in the background
            RunBackground(
                "task_helper_run_background_concurrent_enumerable",
                (childLogger) => RunConcurrentEnumerableAsync(
                    name, list, callback, childLogger, obtainLease, errItemCallback, concurrentLimit, successDelay),
                logger,
                autoLogOperation: false);
        }

        /// <inheritdoc/>
        public Task RunConcurrentEnumerableAsync<T>(
            string name,
            IEnumerable<T> list,
            Func<T, IDiagnosticsLogger, Task> callback,
            IDiagnosticsLogger logger = null,
            Func<T, IDiagnosticsLogger, Task<IDisposable>> obtainLease = null,
            Action<T, Exception, IDiagnosticsLogger> errItemCallback = default,
            int concurrentLimit = 3,
            int successDelay = 250)
        {
            logger = logger ?? Logger.NewChildLogger();

            // When debugging, just run one at a time
            concurrentLimit = Math.Max(concurrentLimit, 1);
            concurrentLimit = Debugger.IsAttached ? 1 : concurrentLimit;

            // Log the main task
            return logger.OperationScopeAsync(
                "task_helper_run_background_concurrent_enumerable",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue("TaskWorkerEnumerableRunId", Guid.NewGuid())
                        .FluentAddBaseValue("TaskEnumerableRunName", name)
                        .FluentAddBaseValue("TasksRunName", name);

                    // Trigger core runner
                    await RunConcurrentEnumerableCoreAsync(
                        name, list, callback, childLogger, obtainLease, errItemCallback, concurrentLimit, successDelay);
                });
        }

        /// <inheritdoc/>
        public async Task<bool> RetryUntilSuccessOrTimeout(
            string name,
            Func<IDiagnosticsLogger, Task<bool>> callback,
            TimeSpan timeoutTimeSpan,
            TimeSpan? waitTimeSpan = null,
            IDiagnosticsLogger logger = null,
            Action onTimeout = null)
        {
            logger = logger ?? Logger.NewChildLogger();

            // Log the main task
            return await logger.OperationScopeAsync(
                "task_helper_run_until_success_or_timeout",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue("TaskWorkerRetryRunId", Guid.NewGuid())
                        .FluentAddBaseValue("TaskRetryRunName", name)
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
                                tryLogger.FluentAddBaseValue("TaskWorkerRetrAttemptRunId", Guid.NewGuid())
                                    .FluentAddValue("TaskRetrAttemptCount", tryCount)
                                    .FluentAddValue("TaskTimeoutTimeSpan", timeoutTimeSpan);

                                // Execute core
                                success = await logger.OperationScopeAsync(name, (opLogger) => callback(opLogger));

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

        private Task<bool> RunBackgroundLoopItemCore(
            string name,
            Func<IDiagnosticsLogger, Task<bool>> callback,
            IDiagnosticsLogger logger,
            bool autoLogOperation,
            Func<Exception, IDiagnosticsLogger, bool> errItemCallback)
        {
            if (autoLogOperation)
            {
                return logger.OperationScopeAsync(
                    name,
                    callback,
                    (e, childLogger) =>
                    {
                        var swallow = errItemCallback != null ? errItemCallback(e, childLogger) : true;
                        return Task.FromResult(swallow);
                    },
                    swallowException: true);
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

                    return Task.FromResult(errItemCallback != null ? errItemCallback(e, logger) : true);
                }
            }
        }

        private Func<Task> RunBackgroundCore(
            string name,
            Func<IDiagnosticsLogger, Task> callback,
            IDiagnosticsLogger logger,
            bool autoLogOperation,
            Func<Exception, IDiagnosticsLogger, Task> errCallback)
        {
            logger = (logger ?? Logger.NewChildLogger())
                .FluentAddBaseValue("TaskBackgroundRunId", Guid.NewGuid())
                .FluentAddBaseValue("TaskBackgroundRunName", name)
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
                            errCallback(e, logger);
                        }

                        return Task.CompletedTask;
                    }
                };
            }
        }

        private async Task RunConcurrentEnumerableCoreAsync<T>(
           string name,
           IEnumerable<T> list,
           Func<T, IDiagnosticsLogger, Task> callback,
           IDiagnosticsLogger logger,
           Func<T, IDiagnosticsLogger, Task<IDisposable>> obtainLease,
           Action<T, Exception, IDiagnosticsLogger> errItemCallback,
           int concurrentLimit,
           int successDelay)
        {
            var semaphoreThrottleWait = new Stopwatch();
            var semaphore = new SemaphoreSlim(concurrentLimit, concurrentLimit);
            var results = new List<TaskCompletionSource<Exception>>();
            var index = 0;

            // Run through each item in the list
            foreach (var item in list)
            {
                // Task tracking
                var localCompletion = new TaskCompletionSource<Exception>();
                results.Add(localCompletion);

                // Spawn work to take place in the background, this allows for the
                // concurrent worker limit to be achived, otherwise we would only
                // be running one at time.
                RunBackground(
                    "task_helper_run_background_enumerable_item",
                    async (itemLogger) =>
                    {
                        itemLogger.FluentAddBaseValue("TaskItemRunId", Guid.NewGuid())
                            .FluentAddValue("IterateItemIndex", index)
                            .FluentAddValue("LockConcurrentLimit", concurrentLimit)
                            .FluentAddValue("LockConcurrentSemaphoreCount", semaphore.CurrentCount);

                        // Wait for worker space to free up
                        var semaphoreWait = Stopwatch.StartNew();
                        await semaphore.WaitAsync();
                        itemLogger.FluentAddDuration("LockConcurrentSemaphore", semaphoreWait);

                        try
                        {
                            // Core task execution
                            await RunEnumerableItemCoreAsync(
                                name, item, callback, itemLogger, obtainLease, successDelay);

                            // Track completion
                            localCompletion.SetResult(null);
                        }
                        catch (Exception e)
                        {
                            // Track completion
                            localCompletion.SetResult(e);

                            throw;
                        }
                        finally
                        {
                            // Release worker space
                            semaphore.Release();
                        }
                    },
                    logger);

                // Throttle the amount of tasks being generated
                if (index % 10 == 0)
                {
                    semaphoreThrottleWait.Start();
                    await Task.WhenAll(results.Select(x => x.Task));
                    semaphoreThrottleWait.Stop();

                    logger.FluentAddDuration("IterateSemaphoreThrottleWait", semaphoreThrottleWait);
                }

                index++;
            }

            // Exception handling
            var exceptions = (await Task.WhenAll(results.Select(x => x.Task)))
                .Where(x => x != null);

            logger.FluentAddValue("IterateItemCount", index)
                .FluentAddValue("IterateExceptionCount", exceptions.Count())
                .FluentAddDuration("IterateSemaphoreThrottleWait", semaphoreThrottleWait);

            // Throw aggregate exception
            if (exceptions.Any())
            {
                throw new AggregateException("Run Background Enumerable items threw excpetions", exceptions);
            }
        }

        private async Task RunEnumerableCoreAsync<T>(
            string name, IEnumerable<T> list, Func<T, IDiagnosticsLogger, Task> callback, IDiagnosticsLogger logger, Func<T, IDiagnosticsLogger, Task<IDisposable>> obtainLease, int successDelay)
        {
            var index = 0;

            // Run through each item in the list
            foreach (var item in list)
            {
                // Track a log statement per item
                await logger.OperationScopeAsync(
                    "task_helper_run_background_enumerable_item",
                    async (itemLogger) =>
                    {
                        itemLogger.FluentAddBaseValue("TaskItemRunId", Guid.NewGuid())
                            .FluentAddValue("IterateItemIndex", index);

                        // Core task execution
                        await RunEnumerableItemCoreAsync(
                            name, item, callback, itemLogger, obtainLease, successDelay);
                    });

                index++;
            }

            logger.FluentAddValue("IterateItemCount", index);
        }

        private async Task<bool> RunEnumerableItemCoreAsync<T>(
            string name,
            T item,
            Func<T, IDiagnosticsLogger, Task> callback,
            IDiagnosticsLogger logger,
            Func<T, IDiagnosticsLogger, Task<IDisposable>> obtainLease,
            int successDelay)
        {
            var success = false;

            logger.FluentAddValue("LeaseShouldObtain", obtainLease != null);

            Func<IDiagnosticsLogger, Task> wrappedCallback = async (childLogger) =>
                {
                    childLogger.FluentAddValue("IterateItemTask", true);
                    await callback(item, childLogger);
                    success = true;
                };

            // If we don't have lease, normal execute
            if (obtainLease == null)
            {
                await logger.OperationScopeAsync(name, wrappedCallback);
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
                        await logger.OperationScopeAsync(name, wrappedCallback);
                    }
                }
            }

            // Pause to give some time between runs (mainly to give other
            // workers on other machines a case to work through things)
            if (success && successDelay > 0)
            {
                var delayDifference = (int)(successDelay * 0.1);
                await Task.Delay(Random.Next(successDelay - delayDifference, successDelay + delayDifference));
            }

            return success;
        }
    }
}