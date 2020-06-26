// <copyright file="RunnerTaskHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LoadRunnerConsoleApp
{
    /// <summary>
    /// Runner Task Helper.
    /// </summary>
    public static class RunnerTaskHelper
    {
        /// <summary>
        /// Continues running a task until its success or timeout occurs.
        /// </summary>
        /// <param name="taskHelper">Target task helper.</param>
        /// <param name="name">Target name.</param>
        /// <param name="callback">Target task.</param>
        /// <param name="timeoutTimeSpan">Target timeout period.</param>
        /// <param name="waitTimeSpan">Target wait time between runs.</param>
        /// <param name="logger">Target logger.</param>
        /// <param name="onTimeout">Action that runs when timeout occurs.</param>
        /// <returns>Returns whether the task was successful.</returns>
        public static Task<bool> HttpRetryUntilSuccessOrTimeout(
            this ITaskHelper taskHelper,
            string name,
            Func<IDiagnosticsLogger, Task<bool>> callback,
            TimeSpan timeoutTimeSpan,
            TimeSpan? waitTimeSpan = null,
            IDiagnosticsLogger logger = null,
            Action onTimeout = null)
        {
            var errorCount = 0;

            return taskHelper.RetryUntilSuccessOrTimeout(
                name,
                async (childLogger) =>
                {
                    try
                    {
                        return await callback(childLogger);
                    }
                    catch (HttpResponseStatusException e)
                        when (e.StatusCode == HttpStatusCode.ServiceUnavailable
                            || e.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        var delay = e.RetryAfter.HasValue ? e.RetryAfter.Value * 1000 : 30000;
                        await Task.Delay(delay);
                    }
                    #pragma warning disable CS0168
                    catch (HttpRequestException e)
                    #pragma warning restore CS0168
                    {
                        // Throw if we have tried too many times
                        if (++errorCount == 3)
                        {
                            throw;
                        }

                        await Task.Delay((int)TimeSpan.FromSeconds(1).TotalMilliseconds);
                    }
                    #pragma warning disable CS0168
                    catch (Exception e)
                    #pragma warning restore CS0168
                    {
                        throw;
                    }

                    return false;
                },
                timeoutTimeSpan,
                waitTimeSpan,
                logger,
                onTimeout);
        }
    }
}
