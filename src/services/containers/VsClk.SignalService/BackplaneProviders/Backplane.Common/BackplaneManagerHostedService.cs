// <copyright file="BackplaneManagerHostedService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService.Common;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Background long running service to wrap the IBackplaneManagerService instance.
    /// </summary>
    public class BackplaneManagerHostedService : BackgroundService
    {
        private const string MethodProcessHealth = "ProcessHealth";
        private const string MethodServiceCounters = "ServiceCounters";

        private const string ServiceMethodPerfScope = "service_method_perf";
        private const string ServiceMethodPerfCountScope = "service_method_perf_count";
        private const string ServiceMethodPerfAverageTimeScope = "service_method_perf_avg_time";

        private const int TimespanUpdateSecs = 5;
        private const int ProcessHealthUpdateSecs = 120;
        private const int ServiceCountersUpdateSecs = 60;

        public BackplaneManagerHostedService(
            IEnumerable<IBackplaneManagerBase> backplaneManagers,
            ILogger<BackplaneManagerHostedService> logger,
            IServiceCounters serviceCounters = null)
        {
            BackplaneManagers = backplaneManagers;
            Logger = logger;
            ServiceCounters = serviceCounters;
        }

        private IServiceCounters ServiceCounters { get; }

        private IEnumerable<IBackplaneManagerBase> BackplaneManagers { get; }

        private ILogger Logger { get; }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogDebug($"StopAsync");

            foreach (var backplaneManager in BackplaneManagers)
            {
                await backplaneManager.DisposeAsync(cancellationToken);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (true)
            {
                await Logger.InvokeWithUnhandledErrorAsync(() => RunAsync(stoppingToken));
            }
        }

        private static string ToString(Dictionary<string, (int, TimeSpan)> methodPerfCounter)
        {
            // Note: this will return something like:
            // method1= NTimes:AvgTime, method2= NTimes:AvgTime, etc..
            // in which NTimes is the number of times the method was invoked and AvgTime is the average time it takes to execute which is => Total Time accumulated/NTimes
            return string.Join(',', methodPerfCounter.Select(kvp => $"{kvp.Key}={kvp.Value.Item1}:{ToAvgTime(kvp.Value)}"));
        }

        private static string ToString(Dictionary<string, Dictionary<string, (int, TimeSpan)>> hubMethodPerfCounters)
        {
            // Note this will return a pattern
            // service1=> [method perfs], service2=> [method perfs]
            // in which [method perfs] is describe previous method.
            return string.Join(',', hubMethodPerfCounters.Select(kvp => $"{kvp.Key}:[{ToString(kvp.Value)}]"));
        }

        private static double ToAvgTime((int, TimeSpan) value)
        {
            return Math.Round(value.Item2.TotalMilliseconds / value.Item1, 2);
        }

        private async Task RunAsync(CancellationToken stoppingToken)
        {
            Logger.LogDebug($"RunAsync");

            await RunBackplaneManagersAsync(true, stoppingToken);

            var updateMetricsCounter = new SecondsCounter(BackplaneManagerConst.UpdateMetricsSecs, TimespanUpdateSecs);
            var logProcessHealthCounter = new SecondsCounter(ProcessHealthUpdateSecs, TimespanUpdateSecs);
            var serviceCountersUpdateCounter = new SecondsCounter(ServiceCountersUpdateSecs, TimespanUpdateSecs);

            while (true)
            {
                // update metrics if factory is defined
                await RunBackplaneManagersAsync(updateMetricsCounter.Next(), stoppingToken);

                if (logProcessHealthCounter.Next())
                {
                    var memoryInfo = LoggerScopeHelpers.GetProcessMemoryInfo();
                    var cpuUsage = await LoggerScopeHelpers.GetCpuUsageForProcessAsync();

                    using (Logger.BeginScope(
                        (LoggerScopeHelpers.MethodScope, MethodProcessHealth),
                        (LoggerScopeHelpers.MemorySizeProperty, memoryInfo.memorySize),
                        (LoggerScopeHelpers.TotalMemoryProperty, memoryInfo.totalMemory),
                        (LoggerScopeHelpers.CpuUsageProperty, cpuUsage)))
                    {
                        Logger.LogInformation($"Health report");
                    }
                }

                if (serviceCountersUpdateCounter.Next() && ServiceCounters != null)
                {
                    var methodPerfCounters = ServiceCounters.GetPerfCounters();
                    foreach (var kvpSrvc in methodPerfCounters)
                    {
                        foreach (var kvpMethod in kvpSrvc.Value)
                        {
                            var scopeKey = kvpMethod.Key.Replace('-', '_');
                            var perfScopes = new List<(string, object)>();
                            perfScopes.Add((ServiceMethodPerfScope, $"{kvpSrvc.Key}_{scopeKey}"));

                            perfScopes.Add((ServiceMethodPerfCountScope, kvpMethod.Value.Item1));
                            if (kvpMethod.Value.Item2 != TimeSpan.Zero)
                            {
                                perfScopes.Add((ServiceMethodPerfAverageTimeScope, ToAvgTime(kvpMethod.Value)));
                            }

                            using (LoggerScopeHelpers.BeginScope(Logger, perfScopes.ToArray()))
                            {
                                Logger.LogInformation($"method:{kvpSrvc.Key}.{kvpMethod.Key} count:{kvpMethod.Value.Item1} total time:{kvpMethod.Value.Item2}");
                            }
                        }
                    }

                    Logger.LogMethodScope(LogLevel.Information, methodPerfCounters.Count > 0 ? $"(Service:[method=events/min:ms]):{ToString(methodPerfCounters)}" : "n/a", MethodServiceCounters);

                    ServiceCounters.ResetCounters();
                }

                // delay
                await Task.Delay(TimeSpan.FromSeconds(TimespanUpdateSecs), stoppingToken);
            }
        }

        private async Task RunBackplaneManagersAsync(bool updateBackplaneMetrics, CancellationToken stoppingToken)
        {
            foreach (var backplaneManager in BackplaneManagers)
            {
                await Logger.InvokeWithUnhandledErrorAsync(
                    () => backplaneManager.HandleNextAsync(updateBackplaneMetrics, stoppingToken),
                    () => $"method:{nameof(RunBackplaneManagersAsync)}, backplaneManager:{backplaneManager.GetType().Name}");
            }
        }
    }
}
