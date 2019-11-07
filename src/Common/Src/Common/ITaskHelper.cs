// <copyright file="ITaskHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Task helper library.
    /// </summary>
    public interface ITaskHelper
    {
        /// <summary>
        /// Runs a TRPL Task fire-and-forget style on a repeated schedule,
        /// the right way - in the background, separate from the current
        /// thread, with no risk of it trying to rejoin the current thread.
        /// </summary>
        /// <param name="name">Target name.</param>
        /// <param name="callback">Target callback.</param>
        /// <param name="schedule">Target time between runs.</param>
        /// <param name="logger">Target logger.</param>
        /// <param name="autoLogLoopOperation">
        /// Whether the task execution of each item should be auto logged.
        /// </param>
        /// <param name="errLoopCallback">
        /// Callback which will trigger when erros happen on execution of each item.
        /// </param>
        void RunBackgroundLoop(
            string name,
            Func<IDiagnosticsLogger, Task<bool>> callback,
            TimeSpan? schedule = null,
            IDiagnosticsLogger logger = null,
            bool autoLogLoopOperation = false,
            Func<Exception, IDiagnosticsLogger, bool> errLoopCallback = default);

        /// <summary>
        /// Runs a TRPL Task fire-and-forget style on a repeated schedule,
        /// the right way - in the background, separate from the current
        /// thread, with no risk of it trying to rejoin the current thread.
        /// The exception to this is the first run of the callback which is
        /// expected to be awaited 
        /// </summary>
        /// <param name="name">Target name.</param>
        /// <param name="callback">Target callback.</param>
        /// <param name="schedule">Target time between runs.</param>
        /// <param name="logger">Target logger.</param>
        /// <param name="autoLogLoopOperation">
        /// Whether the task execution of each item should be auto logged.
        /// </param>
        /// <param name="errLoopCallback">
        /// Callback which will trigger when erros happen on execution of each item.
        /// </param>
        /// <returns>Running tasks.</returns>
        Task RunBackgroundLoopAsync(
            string name,
            Func<IDiagnosticsLogger, Task<bool>> callback,
            TimeSpan? schedule = null,
            IDiagnosticsLogger logger = null,
            bool autoLogLoopOperation = false,
            Func<Exception, IDiagnosticsLogger, bool> errLoopCallback = default);

        /// <summary>
        /// Runs a TPL Task fire-and-forget style, the right way - in the
        /// background, separate from the current thread, with no risk
        /// of it trying to rejoin the current thread.
        /// </summary>
        /// <param name="name">Target name.</param>
        /// <param name="callback">Target callback.</param>
        /// <param name="logger">Target logger.</param>
        /// <param name="autoLogOperation">Whether the task execution should be auto logged.</param>
        /// <param name="errCallback">Callback that will get executed on error.</param>
        /// <param name="delay">Target delay till run.</param>
        void RunBackground(
            string name,
            Func<IDiagnosticsLogger, Task> callback,
            IDiagnosticsLogger logger = null,
            bool autoLogOperation = true,
            Action<Exception, IDiagnosticsLogger> errCallback = default,
            TimeSpan? delay = null);

        /// <summary>
        /// Runs a task fire-and-forget style and notifies the TPL that this
        /// will not need a Thread to resume on for a long time, or that there
        /// are multiple gaps in thread use that may be long.
        /// Use for example when talking to a slow webservice.
        /// </summary>
        /// <param name="name">Target name.</param>
        /// <param name="callback">Target callback.</param>
        /// <param name="logger">Target logger.</param>
        /// <param name="autoLogOperation">Whether the task execution should be auto logged.</param>
        /// <param name="errCallback">Callback that will get executed on error.</param>
        /// <param name="delay">Target delay till run.</param>
        void RunBackgroundLong(
            string name,
            Func<IDiagnosticsLogger, Task> callback,
            IDiagnosticsLogger logger = null,
            bool autoLogOperation = true,
            Action<Exception, IDiagnosticsLogger> errCallback = default,
            TimeSpan? delay = null);

        /// <summary>
        /// In serial, iterates through each item in the enumerable in the background.
        /// </summary>
        /// <typeparam name="T">Type of the item enumerable.</typeparam>
        /// <param name="name">Target name.</param>
        /// <param name="list">Target list.</param>
        /// <param name="callback">Target callback that will be once item in the enumeration.</param>
        /// <param name="logger">Target logger.</param>
        /// <param name="obtainLease">
        /// Target lease, that if provided, will be obtained once per item in the enumeration.
        /// </param>
        /// <param name="itemDelay">
        /// Target delay, that if provided, will space out successful executions. This is intended to allow other
        /// workers on other machines a chance to work through the items (used mainly in conjuntion with a
        /// distributed leas).
        /// </param>
        void RunBackgroundEnumerable<T>(
           string name,
           IEnumerable<T> list,
           Func<T, IDiagnosticsLogger, Task> callback,
           IDiagnosticsLogger logger = null,
           Func<T, IDiagnosticsLogger, Task<IDisposable>> obtainLease = null,
           int itemDelay = 250);

        /// <summary>
        /// In serial, iterates through each item in the enumerable.
        /// </summary>
        /// <typeparam name="T">Type of the item enumerable.</typeparam>
        /// <param name="name">Target name.</param>
        /// <param name="list">Target list.</param>
        /// <param name="callback">Target callback that will be once item in the enumeration.</param>
        /// <param name="logger">Target logger.</param>
        /// <param name="obtainLease">
        /// Target lease, that if provided, will be obtained once per item in the enumeration.
        /// </param>
        /// <param name="itemDelay">
        /// Target delay, that if provided, will space out successful executions. This is intended to allow other
        /// workers on other machines a chance to work through the items (used mainly in conjuntion with a
        /// distributed leas).
        /// </param>
        /// <returns>Running tasks.</returns>
        Task RunEnumerableAsync<T>(
            string name,
            IEnumerable<T> list,
            Func<T, IDiagnosticsLogger, Task> callback,
            IDiagnosticsLogger logger = null,
            Func<T, IDiagnosticsLogger, Task<IDisposable>> obtainLease = null,
            int itemDelay = 250);

        /// <summary>
        /// Simple, lightweight worker implementation that allows for x amount of items in an enumeration
        /// to be worked on at once.
        /// </summary>
        /// <typeparam name="T">Type of the item enumerable.</typeparam>
        /// <param name="name">Target name.</param>
        /// <param name="list">Target list.</param>
        /// <param name="callback">Target callback that will be once item in the enumeration.</param>
        /// <param name="logger">Target logger.</param>
        /// <param name="obtainLease">
        /// Target lease, that if provided, will be obtained once per item in the enumeration.
        /// </param>
        /// <param name="errItemCallback">Callback that will be run on execution of each item.</param>
        /// <param name="concurrentLimit">Target number of concurrent workers that will run.</param>
        /// <param name="successDelay">
        /// Target delay, that if provided, will space out successful executions. This is intended to allow other
        /// workers on other machines a chance to work through the items (used mainly in conjuntion with a
        /// distributed leas).
        /// </param>
        void RunBackgroundConcurrentEnumerable<T>(
            string name,
            IEnumerable<T> list,
            Func<T, IDiagnosticsLogger, Task> callback,
            IDiagnosticsLogger logger = null,
            Func<T, IDiagnosticsLogger, Task<IDisposable>> obtainLease = null,
            Action<T, Exception, IDiagnosticsLogger> errItemCallback = default,
            int concurrentLimit = 3,
            int successDelay = 250);

        /// <summary>
        /// Simple, lightweight worker implementation that allows for x amount of items in an enumeration
        /// to be worked on at once.
        /// </summary>
        /// <typeparam name="T">Type of the item enumerable.</typeparam>
        /// <param name="name">Target name.</param>
        /// <param name="list">Target list.</param>
        /// <param name="callback">Target callback that will be once item in the enumeration.</param>
        /// <param name="logger">Target logger.</param>
        /// <param name="obtainLease">
        /// Target lease, that if provided, will be obtained once per item in the enumeration.
        /// </param>
        /// <param name="errItemCallback">Callback that will be run on execution of each item.</param>
        /// <param name="concurrentLimit">Target number of concurrent workers that will run.</param>
        /// <param name="successDelay">
        /// Target delay, that if provided, will space out successful executions. This is intended to allow other
        /// workers on other machines a chance to work through the items (used mainly in conjuntion with a
        /// distributed leas).
        /// </param>
        /// <returns>Running tasks.</returns>
        Task RunConcurrentEnumerableAsync<T>(
            string name,
            IEnumerable<T> list,
            Func<T, IDiagnosticsLogger, Task> callback,
            IDiagnosticsLogger logger = null,
            Func<T, IDiagnosticsLogger, Task<IDisposable>> obtainLease = null,
            Action<T, Exception, IDiagnosticsLogger> errItemCallback = default,
            int concurrentLimit = 3,
            int successDelay = 250);

        /// <summary>
        /// Continues running a task until its success or timeout occurs.
        /// </summary>
        /// <param name="name">Target name.</param>
        /// <param name="callback">Target task.</param>
        /// <param name="timeoutTimeSpan">Target timeout period.</param>
        /// <param name="waitTimeSpan">Target wait time between runs.</param>
        /// <param name="logger">Target logger.</param>
        /// <param name="onTimeout">Action that runs when timeout occurs.</param>
        /// <returns>Returns whether the task was successful.</returns>
        Task<bool> RetryUntilSuccessOrTimeout(
            string name,
            Func<IDiagnosticsLogger, Task<bool>> callback,
            TimeSpan timeoutTimeSpan,
            TimeSpan? waitTimeSpan = null,
            IDiagnosticsLogger logger = null,
            Action onTimeout = null);
    }
}