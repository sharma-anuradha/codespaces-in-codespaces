// <copyright file="LoggingExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;

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
    }
}
