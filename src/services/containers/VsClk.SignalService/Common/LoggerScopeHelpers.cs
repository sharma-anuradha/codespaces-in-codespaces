// <copyright file="LoggerScopeHelpers.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsCloudKernel.SignalService.Common
{
    /// <summary>
    /// Helper class to allow pushing a scope usign a key value pair
    /// </summary>
    public static class LoggerScopeHelpers
    {
        public const string MethodScope = "Method";
        public const string MethodPerfScope = "MethodPerf";
        public const string MemorySizeProperty = "MemorySize";
        public const string TotalMemoryProperty = "TotalMemory";

        public static IDisposable BeginScope(this ILogger logger, params (string, object)[] scopes)
        {
            Requires.NotNull(logger, nameof(logger));

            var loggerScope = new JObject();
            foreach (var scope in scopes)
            {
                loggerScope[scope.Item1] = scope.Item2 != null ? JToken.FromObject(scope.Item2) : JValue.CreateNull();
            }

            return logger.BeginScope(loggerScope);
        }

        public static IDisposable BeginSingleScope(this ILogger logger, (string, object) scope)
        {
            return BeginScope(logger, new (string, object)[] { scope });
        }

        public static IDisposable BeginMethodScope(this ILogger logger, string methodScope)
        {
            return logger.BeginSingleScope((MethodScope, methodScope));
        }

        public static IDisposable BeginMethodScope(this ILogger logger, string methodScope, long methodPerf)
        {
            return logger.BeginScope((MethodScope, methodScope), (MethodPerfScope, methodPerf));
        }

        public static void LogScope(this ILogger logger, LogLevel logLevel, string message, params (string, object)[] scopes)
        {
            using (BeginScope(logger, scopes))
            {
                logger.Log(logLevel, message);
            }
        }

        public static void LogMethodScope(this ILogger logger, LogLevel logLevel, string message, string methodScope)
        {
            using (BeginMethodScope(logger, methodScope))
            {
                logger.Log(logLevel, message);
            }
        }

        public static void LogMethodScope(this ILogger logger, LogLevel logLevel, Exception exception, string message, string methodScope)
        {
            using (BeginMethodScope(logger, methodScope))
            {
                logger.Log(logLevel, exception, message);
            }
        }

        public static void LogMethodScope(this ILogger logger, LogLevel logLevel, string message, string methodScope, long methodPerf)
        {
            using (BeginMethodScope(logger, methodScope, methodPerf))
            {
                logger.Log(logLevel, message);
            }
        }

        public static (long memorySize, long totalMemory) GetProcessMemoryInfo()
        {
            const long OneMb = 1024 * 1024;

            using (var proc = Process.GetCurrentProcess())
            {
                return (proc.WorkingSet64 / OneMb, GC.GetTotalMemory(false) / OneMb);
            }
        }
    }
}
