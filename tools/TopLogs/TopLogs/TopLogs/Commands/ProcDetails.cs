using CommandLine;
using System.IO;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.VsSaaS.Tools.TopLogs.Models;

namespace Microsoft.VsSaaS.Tools.TopLogs.Commands
{
    [Verb("proc-details", HelpText = "Show details of the given process")]
    public class ProcDetails : CommandBase
    {
        [Option('p', "pid", Required = true, HelpText = "Pid")]
        public int Pid { get; set; }

        public int Execute(TextWriter stdout, TextWriter stderr)
        {
            var logs = GetLogs(stdout, stderr);

            var aggregate = default(ProcessAggregateInfo);
            var details = new List<(TopLog Log, ProcessDetails Details)>();

            foreach (var log in logs.OrderBy(log => log.Time))
            {
                var proc = log.Processes.FirstOrDefault(p => p.Pid == Pid);
                if (proc == null)
                {
                    continue;
                }

                details.Add((log, proc));

                if (aggregate == default)
                {
                    aggregate = new ProcessAggregateInfo()
                    {
                        Command = proc.Command,
                        User = proc.User,
                        MaxCpu = proc.CpuUsage,
                        MinCpu = proc.CpuUsage,
                        MaxMem = proc.MemUsage,
                        MinMem = proc.MemUsage,
                        StartTime = log.Time,
                        EndTime = log.Time,
                    };
                }
                else
                {
                    aggregate.MaxCpu = Math.Max(aggregate.MaxCpu, proc.CpuUsage);
                    aggregate.MinCpu = Math.Min(aggregate.MinCpu, proc.CpuUsage);

                    aggregate.MaxMem = Math.Max(aggregate.MaxMem, proc.MemUsage);
                    aggregate.MinMem = Math.Min(aggregate.MinMem, proc.MemUsage);

                    aggregate.EndTime = log.Time;
                }
            }

            Output(aggregate, details, stdout);            

            return 0;
        }

        private void Output(ProcessAggregateInfo aggregate, List<(TopLog Log, ProcessDetails Details)> details, TextWriter stdout)
        {
            stdout.WriteLine($"Pid: {Pid}");
            stdout.WriteLine($"Command: {aggregate.Command}");
            stdout.WriteLine($"User: {aggregate.User}");
            stdout.WriteLine($"%Cpu: min={aggregate.MinCpu}, max={aggregate.MaxCpu}");
            stdout.WriteLine($"%Mem: min={aggregate.MinMem}, max={aggregate.MaxMem}");

            stdout.WriteLine();

            stdout.WriteLine(string.Join("\t", "TIME", "MS", "CPU", "MEM"));

            var first = details.First();

            foreach (var (log, detail) in details)
            {
                var msSinceStart = (int)(log.Time - first.Log.Time).TotalMilliseconds;

                stdout.WriteLine(string.Join("\t", log.Time, msSinceStart, detail.CpuUsage, detail.MemUsage));
            }
        }

        private class ProcessAggregateInfo
        {
            public string Command;
            public string User;

            public double MaxCpu;
            public double MinCpu;

            public double MaxMem;
            public double MinMem;

            public DateTime StartTime;
            public DateTime EndTime;
        }
    }
}
