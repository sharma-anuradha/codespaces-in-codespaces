// <copyright file="LoggingExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Construct consistent logging messages.
/// </summary>
/// <remarks>
/// TODO: move to VS SaaS SDK Diagnostics.
/// </remarks>
namespace Microsoft.VsSaaS.Diagnostics.Extensions
{
    /// <summary>
    /// Support for creating well-formed logging messages.
    /// </summary>
    public static class LoggingExtensions
    {
        /// <summary>
        /// Generates a new child logger based off the parent.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>New child logger.</returns>
        public static IDiagnosticsLogger NewChildLogger(this IDiagnosticsLogger logger)
        {
            return logger.WithValues(new LogValueSet());
        }

        /// <summary>
        /// Wraps the given operation in a logging scope which will catch and log exceptions
        /// as well as the successful completeion of the operation.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <param name="name">Base name of the operaiton scope.</param>
        /// <param name="callback">Callback that should be executed.</param>
        /// <param name="errCallback">Callback that should be executed if an exception occurs.</param>
        /// <param name="swallowException">Whether any exceptions should be swallowed.</param>
        /// <returns>Returns the task.</returns>
        public static async Task OperationScopeAsync(
            this IDiagnosticsLogger logger,
            string name,
            Func<IDiagnosticsLogger, Task> callback,
            Action<Exception, IDiagnosticsLogger> errCallback = default,
            bool swallowException = false)
        {
            var childLogger = logger.WithValues(new LogValueSet());
            var duration = Stopwatch.StartNew();

            try
            {
                await callback(childLogger);

                childLogger.FluentAddDuration(duration).LogInfo($"{name}_complete");
            }
            catch (Exception e)
            {
                errCallback?.Invoke(e, childLogger);

                childLogger.FluentAddDuration(duration).LogException($"{name}_error", e);

                if (!swallowException)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Wraps the given operation in a logging scope which will catch and log exceptions
        /// as well as the successful completeion of the operation.
        /// </summary>
        /// <typeparam name="T">Return type.</typeparam>
        /// <param name="logger">Target logger.</param>
        /// <param name="name">Base name of the operaiton scope.</param>
        /// <param name="callback">Callback that should be executed.</param>
        /// <param name="errCallback">Callback that should be executed if an exception occurs.</param>
        /// <param name="swallowException">Whether any exceptions shouldbe swallowed.</param>
        /// <returns>Returns the task.</returns>
        public static async Task<T> OperationScopeAsync<T>(
            this IDiagnosticsLogger logger,
            string name,
            Func<IDiagnosticsLogger, Task<T>> callback,
            Func<Exception, IDiagnosticsLogger, T> errCallback = default,
            bool swallowException = false)
        {
            var result = default(T);

            await logger.OperationScopeAsync(
                name,
                async (innerLogger) => { result = await callback(innerLogger); },
                (e, innerLogger) =>
                {
                    if (errCallback != default)
                    {
                        result = errCallback(e, innerLogger);
                    }
                },
                swallowException);

            return result;
        }

        /// <summary>
        /// Wraps the given operation in a logging scope which will catch and log exceptions
        /// as well as the successful completeion of the operation.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <param name="name">Base name of the operaiton scope.</param>
        /// <param name="callback">Callback that should be executed.</param>
        /// <param name="errCallback">Callback that should be executed if an exception occurs.</param>
        /// <param name="swallowException">Whether any exceptions shouldbe swallowed.</param>
        public static void OperationScope(
            this IDiagnosticsLogger logger,
            string name,
            Action<IDiagnosticsLogger> callback,
            Action<Exception, IDiagnosticsLogger> errCallback = default,
            bool swallowException = false)
        {
            var childLogger = logger.WithValues(new LogValueSet());
            var duration = Stopwatch.StartNew();

            try
            {
                callback(childLogger);

                childLogger.FluentAddDuration(duration).LogInfo($"{name}_complete");
            }
            catch (Exception e)
            {
                errCallback?.Invoke(e, childLogger);

                childLogger.FluentAddDuration(duration).LogException($"{name}_error", e);

                if (!swallowException)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Wraps the given operation in a logging scope which will catch and log exceptions
        /// as well as the successful completeion of the operation.
        /// </summary>
        /// <typeparam name="T">Return type.</typeparam>
        /// <param name="logger">Target logger.</param>
        /// <param name="name">Base name of the operaiton scope.</param>
        /// <param name="callback">Callback that should be executed.</param>
        /// <param name="errCallback">Callback that should be executed if an exception occurs.</param>
        /// <param name="swallowException">Whether any exceptions shouldbe swallowed.</param>
        /// <returns>Returns the callback result.</returns>
        public static T OperationScope<T>(
            this IDiagnosticsLogger logger,
            string name,
            Func<IDiagnosticsLogger, T> callback,
            Func<Exception, IDiagnosticsLogger, T> errCallback = default,
            bool swallowException = false)
        {
            var result = default(T);

            logger.OperationScope(
                name,
                (innerLogger) => { result = callback(innerLogger); },
                (e, innerLogger) =>
                {
                    if (errCallback != default)
                    {
                        result = errCallback(e, innerLogger);
                    }
                },
                swallowException);

            return result;
        }

        /// <summary>
        /// Wraps the given operation in a logging scope which will catch and log exceptions
        /// as well as the successful completeion of the operation. In the case of a fail,
        /// retries will occur.
        /// </summary>
        /// <typeparam name="T">Return type.</typeparam>
        /// <param name="logger">Target logger.</param>
        /// <param name="name">Base name of the operaiton scope.</param>
        /// <param name="callback">Callback that should be executed.</param>
        /// <param name="errCallback">Callback that should be executed if an exception occurs.</param>
        /// <param name="swallowException">Whether any exceptions shouldbe swallowed.</param>
        /// <returns>Returns the callback result.</returns>
        public static async Task<T> RetryOperationScopeAsync<T>(
            this IDiagnosticsLogger logger,
            string name,
            Func<IDiagnosticsLogger, Task<T>> callback,
            Func<Exception, IDiagnosticsLogger, T> errCallback = default,
            bool swallowException = false)
        {
            var result = default(T);

            await logger.RetryOperationScopeAsync(
                name,
                async (innerLogger) => { result = await callback(innerLogger); },
                (e, innerLogger) =>
                {
                    if (errCallback != default)
                    {
                        result = errCallback(e, innerLogger);
                    }
                },
                swallowException);

            return result;
        }

        /// <summary>
        /// Wraps the given operation in a logging scope which will catch and log exceptions
        /// as well as the successful completeion of the operation. In the case of a fail,
        /// retries will occur.
        /// </summary>
        /// <typeparam name="T">Return type.</typeparam>
        /// <param name="logger">Target logger.</param>
        /// <param name="name">Base name of the operaiton scope.</param>
        /// <param name="callback">Callback that should be executed.</param>
        /// <param name="errCallback">Callback that should be executed if an exception occurs.</param>
        /// <param name="swallowException">Whether any exceptions shouldbe swallowed.</param>
        /// <returns>Returns running task.</returns>
        public static async Task RetryOperationScopeAsync(
            this IDiagnosticsLogger logger,
            string name,
            Func<IDiagnosticsLogger, Task> callback,
            Action<Exception, IDiagnosticsLogger> errCallback = default,
            bool swallowException = false)
        {
            var childLogger = logger.WithValues(new LogValueSet());
            var duration = Stopwatch.StartNew();

            // Try executing result
            var doResult = await Retry.DoAsync(
                (int attemptNumber) =>
                {
                    return childLogger.OperationScopeAsync(
                        name,
                        async (innerLogger) =>
                        {
                            innerLogger.FluentAddBaseValue("AttemptNumber", attemptNumber)
                                .FluentAddDuration("AttemptIterationOffset", duration)
                                .FluentAddValue("AttemptIsProgress", attemptNumber != (Retry.DefaultMaxRetryOperationCount - 1));

                            await callback(innerLogger);

                            return (true, null);
                        },
                        (e, innerLogger) => (false, e),
                        true);
                });

            // Log out overall results
            childLogger.FluentAddDuration(duration);
            if (doResult.Item1)
            {
                childLogger.LogInfo($"{name}_retry_scope_complete");
            }
            else
            {
                if (errCallback != default)
                {
                    errCallback(doResult.Item2, childLogger);
                }

                childLogger.LogException($"{name}_retry_scope_error", doResult.Item2);

                if (!swallowException)
                {
                    throw doResult.Item2;
                }
            }
        }

        /// <summary>
        /// Wraps the given operation in a logging scope will add duration to the logger.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <param name="name">Base name of the operaiton scope.</param>
        /// <param name="callback">Callback that should be executed.</param>
        /// <returns>Returns the task.</returns>
        public static async Task TrackDurationAsync(this IDiagnosticsLogger logger, string name, Func<Task> callback)
        {
            var duration = Stopwatch.StartNew();

            try
            {
                await callback();
            }
            finally
            {
                logger.FluentAddDuration(name, duration);
            }
        }

        /// <summary>
        /// Wraps the given operation in a logging scope will add duration to the logger.
        /// </summary>
        /// <typeparam name="T">Return type.</typeparam>
        /// <param name="logger">Target logger.</param>
        /// <param name="name">Base name of the operaiton scope.</param>
        /// <param name="callback">Callback that should be executed.</param>
        /// <returns>Returns the task.</returns>
        public static async Task<T> TrackDurationAsync<T>(this IDiagnosticsLogger logger, string name, Func<Task<T>> callback)
        {
            var duration = Stopwatch.StartNew();

            try
            {
                return await callback();
            }
            finally
            {
                logger.FluentAddDuration(name, duration);
            }
        }

        /// <summary>
        /// Wraps the given operation in a logging scope will add duration to the logger.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <param name="name">Base name of the operaiton scope.</param>
        /// <param name="callback">Callback that should be executed.</param>
        public static void TrackDuration(this IDiagnosticsLogger logger, string name, Action callback)
        {
            var duration = Stopwatch.StartNew();

            try
            {
                callback();
            }
            finally
            {
                logger.FluentAddDuration(name, duration);
            }
        }

        /// <summary>
        /// Wraps the given operation in a logging scope will add duration to the logger.
        /// </summary>
        /// <typeparam name="T">Return type.</typeparam>
        /// <param name="logger">Target logger.</param>
        /// <param name="name">Base name of the operaiton scope.</param>
        /// <param name="callback">Callback that should be executed.</param>
        /// <returns>Returns the callback result.</returns>
        public static T TrackDuration<T>(this IDiagnosticsLogger logger, string name, Func<T> callback)
        {
            var duration = Stopwatch.StartNew();

            try
            {
                return callback();
            }
            finally
            {
                logger.FluentAddDuration(name, duration);
            }
        }

        /// <summary>
        /// Adds duration to the logger.
        /// </summary>
        /// <typeparam name="T">Type of the value.</typeparam>
        /// <param name="logger">Target logger.</param>
        /// <param name="value">Value being set.</param>
        /// <returns>Logger to be used next.</returns>
        public static IDiagnosticsLogger FluentAddDuration(this IDiagnosticsLogger logger, Stopwatch value)
        {
            return logger.FluentAddValue("Duration", value.Elapsed);
        }

        /// <summary>
        /// Adds duration to the logger.
        /// </summary>
        /// <typeparam name="T">Type of the value.</typeparam>
        /// <param name="logger">Target logger.</param>
        /// <param name="name">Name of the property that should be postfixed.</param>
        /// <param name="value">Value being set.</param>
        /// <returns>Logger to be used next.</returns>
        public static IDiagnosticsLogger FluentAddDuration(this IDiagnosticsLogger logger, string name, Stopwatch value)
        {
            return logger.FluentAddValue($"{name}Duration", value.Elapsed);
        }

        /// <summary>
        /// Adds key/value pair to the logger.
        /// </summary>
        /// <typeparam name="T">Type of the value.</typeparam>
        /// <param name="logger">Target logger.</param>
        /// <param name="name">Name of the property.</param>
        /// <param name="value">Value being set.</param>
        /// <returns>Logger to be used next.</returns>
        public static IDiagnosticsLogger FluentAddValue<T>(this IDiagnosticsLogger logger, string name, T value)
            where T : struct
        {
            return logger.FluentAddValue(name, value.ToString());
        }

        /// <summary>
        /// Adds key/value pair to the logger.
        /// </summary>
        /// <typeparam name="T">Type of the value.</typeparam>
        /// <param name="logger">Target logger.</param>
        /// <param name="name">Name of the property.</param>
        /// <param name="value">Value being set.</param>
        /// <returns>Logger to be used next.</returns>
        public static IDiagnosticsLogger FluentAddValue<T>(this IDiagnosticsLogger logger, string name, T? value)
            where T : struct
        {
            return logger.FluentAddValue(name, value.HasValue ? value.Value.ToString() : null);
        }

        /// <summary>
        /// Adds key/value pair to the logger.
        /// </summary>
        /// <typeparam name="T">Type of the value.</typeparam>
        /// <param name="logger">Target logger.</param>
        /// <param name="name">Name of the property.</param>
        /// <param name="value">Value being set.</param>
        /// <returns>Logger to be used next.</returns>
        public static IDiagnosticsLogger FluentAddBaseValue<T>(this IDiagnosticsLogger logger, string name, T value)
            where T : struct
        {
            return logger.FluentAddBaseValue(name, value.ToString());
        }

        /// <summary>
        /// Adds key/value pair to the logger.
        /// </summary>
        /// <typeparam name="T">Type of the value.</typeparam>
        /// <param name="logger">Target logger.</param>
        /// <param name="name">Name of the property.</param>
        /// <param name="value">Value being set.</param>
        /// <returns>Logger to be used next.</returns>
        public static IDiagnosticsLogger FluentAddBaseValue<T>(this IDiagnosticsLogger logger, string name, T? value)
            where T : struct
        {
            return logger.FluentAddBaseValue(name, value.HasValue ? value.Value.ToString() : null);
        }

        /// <summary>
        /// Adds multiple key/values from the input dictionary.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <param name="keyValues">Target key/values.</param>
        /// <returns>Logger to be used next.</returns>
        public static IDiagnosticsLogger FluentAddValues(this IDiagnosticsLogger logger, IDictionary<string, string> keyValues)
        {
            if (keyValues != null)
            {
                foreach (var loggerProperty in keyValues)
                {
                    logger.FluentAddValue(loggerProperty.Key, loggerProperty.Value);
                }
            }

            return logger;
        }

        /// <summary>
        /// Gets the logging base name for the given type. Returns the value of the [LogginBaseName] attribute, or if not specified, the type name if not.
        /// </summary>
        /// <param name="class">The implementation class type that emitst a logging message.</param>
        /// <returns>The logging message base name.</returns>
        public static string GetLogMessageBaseName(this Type @class)
        {
            Requires.NotNull(@class, nameof(@class));
            var loggingBaseNameAttribute = @class.GetCustomAttributes(typeof(LoggingBaseNameAttribute), inherit: true).OfType<LoggingBaseNameAttribute>().FirstOrDefault();
            var loggingBaseName = loggingBaseNameAttribute?.LoggingBaseName;
            loggingBaseName = loggingBaseName ?? @class.Name;
            return loggingBaseName.ToLowerInvariant();
        }

        /// <summary>
        /// Format a log info message.
        /// </summary>
        /// <param name="class">The implementation class type that emitst a logging message.</param>
        /// <param name="methodName">The method name that emits a logging mesage.</param>
        /// <returns>The log message string.</returns>
        public static string FormatLogMessage(this Type @class, string methodName)
        {
            return @class.FormatLogMessage(methodName, false);
        }

        /// <summary>
        /// Format a log info message.
        /// </summary>
        /// <param name="class">The implementation class type that emitst a logging message.</param>
        /// <param name="methodName">The method name that emits a logging mesage.</param>
        /// <returns>The log error message string.</returns>
        public static string FormatLogErrorMessage(this Type @class, string methodName)
        {
            return @class.FormatLogMessage(methodName, true);
        }

        /// <summary>
        /// Format a log info message.
        /// </summary>
        /// <param name="class">The implementation class type that emitst a logging message.</param>
        /// <param name="methodName">The method name that emits a logging mesage.</param>
        /// <param name="isError">A value indicating whether the message represents an error or exception.</param>
        /// <returns>The log message string.</returns>
        private static string FormatLogMessage(this Type @class, string methodName, bool isError = false)
        {
            var loggingBaseName = @class.GetLogMessageBaseName();
            var methodNamePart = string.IsNullOrEmpty(methodName) ? string.Empty : $"_{methodName}";
            var errorPart = !isError ? string.Empty : "_error";
            return $"{loggingBaseName}{methodNamePart}{errorPart}".ToLowerInvariant();
        }
    }
}
