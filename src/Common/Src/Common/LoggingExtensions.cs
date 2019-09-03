// <copyright file="LoggingExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

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
        /// Wraps the given operation in a logging scope which will catch and log exceptions
        /// as well as the successful completeion of the operation.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <param name="name">Base name of the operaiton scope.</param>
        /// <param name="callback">Callback that should be executed.</param>
         /// <param name="errCallback">Callback that should be executed if an exception occurs.</param>
        /// <param name="swallowException">Whether any exceptions shouldbe swallowed.</param>
        /// <returns>Returns the task.</returns>
        public static async Task OperationScopeAsync(this IDiagnosticsLogger logger, string name, Func<Task> callback, Action<Exception> errCallback = default, bool swallowException = false)
        {
            var duration = Stopwatch.StartNew();

            try
            {
                await callback();

                logger.FluentAddValue("Duration", duration.ElapsedMilliseconds).LogInfo($"{name}-complete");
            }
            catch (Exception e)
            {
                errCallback?.Invoke(e);

                logger.FluentAddValue("Duration", duration.ElapsedMilliseconds).LogException($"{name}-error", e);

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
        public static async Task<T> OperationScopeAsync<T>(this IDiagnosticsLogger logger, string name, Func<Task<T>> callback, Func<Exception, T> errCallback = default, bool swallowException = false)
        {
            var duration = Stopwatch.StartNew();
            var result = default(T);

            try
            {
                result = await callback();

                logger.FluentAddValue("Duration", duration.ElapsedMilliseconds).LogInfo($"{name}-complete");
            }
            catch (Exception e)
            {
                if (errCallback != default)
                {
                    result = errCallback(e);
                }

                logger.FluentAddValue("Duration", duration.ElapsedMilliseconds).LogException($"{name}-error", e);

                if (!swallowException)
                {
                    throw;
                }
            }

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
        public static void OperationScope(this IDiagnosticsLogger logger, string name, Action callback, Action<Exception> errCallback = default, bool swallowException = false)
        {
            var duration = logger.StartDuration();

            try
            {
                callback();

                logger.AddDuration(duration).LogInfo($"{name}-complete");
            }
            catch (Exception e)
            {
                errCallback?.Invoke(e);

                logger.AddDuration(duration).LogException($"{name}-error", e);

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
        public static T OperationScope<T>(this IDiagnosticsLogger logger, string name, Func<T> callback, Func<Exception, T> errCallback = default, bool swallowException = false)
        {
            var duration = logger.StartDuration();
            var result = default(T);

            try
            {
                result = callback();

                logger.AddDuration(duration).LogInfo($"{name}-complete");
            }
            catch (Exception e)
            {
                if (errCallback != default)
                {
                    result = errCallback(e);
                }

                logger.AddDuration(duration).LogException($"{name}-error", e);

                if (!swallowException)
                {
                    throw;
                }
            }

            return result;
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
        /// Starts a time when invokes and when disposed, stops the timer and adds the property.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <param name="name">Name of the property.</param>
        /// <returns>Disposable that terminates the timer.</returns>
        public static IDisposable TrackDuration(this IDiagnosticsLogger logger, string name)
        {
            var sw = Stopwatch.StartNew();

            return ActionDisposable.Create(() => logger.FluentAddValue(name, sw.ElapsedMilliseconds));
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
        /// <param name="isError">A value indicating whether the message represents an error or exception </param>
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
