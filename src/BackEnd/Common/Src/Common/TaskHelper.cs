// <copyright file="TaskHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    ///
    /// </summary>
    public class TaskHelper : ITaskHelper
    {
        public TaskHelper(
            IDiagnosticsLogger logger)
        {
            Logger = logger;
        }

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
    }
}