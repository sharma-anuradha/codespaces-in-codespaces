// <copyright file="LoggerScopeHelpers.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsCloudKernel.SignalService.Common
{
    /// <summary>
    /// Helper class to allow pushing a scope usign a key value pair.
    /// </summary>
    public static class LoggerScopeHelpers
    {
        public const string MethodScope = "Method";
        public const string MethodPerfScope = "MethodPerf";
        public const string MemorySizeProperty = "MemorySize";
        public const string TotalMemoryProperty = "TotalMemory";
        public const string CpuUsageProperty = "CpuUsage";

        private const string MethodUnhandledException = "UnhandledException";

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

            /**
             * Note: uncomment this code if you want to aggresively run the GC to track memory leaks.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
             */
            using (var proc = Process.GetCurrentProcess())
            {
                return (proc.WorkingSet64 / OneMb, GC.GetTotalMemory(false) / OneMb);
            }
        }

        public static async Task<bool> InvokeWithUnhandledErrorAsync(this ILogger logger, Func<Task> callback, Func<string> contextCallback = null)
        {
            Assumes.NotNull(callback);

            try
            {
                await callback();
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception error)
            {
                logger.LogMethodScope(LogLevel.Error, error, $"Unhandled exception on context:[{(contextCallback == null ? "null" : contextCallback())}]", MethodUnhandledException);
                return false;
            }
        }

        public static async Task<double> GetCpuUsageForProcessAsync()
        {
            var startTime = DateTime.UtcNow;
            var startCpuUsage = GetCurrentProcessProcessorTime();

            await Task.Delay(500);

            var endTime = DateTime.UtcNow;
            var endCpuUsage = GetCurrentProcessProcessorTime();

            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;

            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

            return cpuUsageTotal * 100;
        }

        public static string GetFriendlyName(this Type type)
        {
            string friendlyName = type.Name;
            if (type.IsGenericType)
            {
                int iBacktick = friendlyName.IndexOf('`');
                if (iBacktick > 0)
                {
                    friendlyName = friendlyName.Remove(iBacktick);
                }

                friendlyName += "<";
                Type[] typeParameters = type.GetGenericArguments();
                for (int i = 0; i < typeParameters.Length; ++i)
                {
                    string typeParamName = GetFriendlyName(typeParameters[i]);
                    friendlyName += i == 0 ? typeParamName : "," + typeParamName;
                }

                friendlyName += ">";
            }

            return friendlyName;
        }

        private static TimeSpan GetCurrentProcessProcessorTime()
        {
            using (var proc = Process.GetCurrentProcess())
            {
                return proc.TotalProcessorTime;
            }
        }
    }
}
