using CommandLine;
using System.IO;
using System;
using System.Linq;
using System.Collections.Generic;
using CommandLine.Text;

namespace Microsoft.VsSaaS.Tools.TopLogs.Commands
{
    [Verb("find-proc", HelpText = "Search for processes matching filters")]
    public class FindProc : CommandBase
    {
        [Option('p', "pid", Default = null, HelpText = "Pid")]
        public int? Pid { get; set; }

        [Option('c', "command", Default = null, HelpText = "Command name")]
        public string Command { get; set; }

        [Option('u', "user", Default = null, HelpText = "User name")]
        public string User { get; set; }

        [Option("cpu-min", Default = null, HelpText = "CPU minimum overall usage, supports comparisons.")]
        public string CpuMin { get; set; }

        [Option("cpu-max", Default = null, HelpText = "CPU maximum overall usage, supports comparisons.")]
        public string CpuMax { get; set; }

        [Option("mem-min", Default = null, HelpText = "Memory minimum overall usage, supports comparisons.")]
        public string MemMin { get; set; }

        [Option("mem-max", Default = null, HelpText = "Memory maximum overall usage, supports comparisons.")]
        public string MemMax { get; set; }

        [Option("start-time", Default = null, HelpText = "Process start time. Value must be a DateTime, supports comparisons.")]
        public string StartTime { get; set; }

        [Option("end-time", Default = null, HelpText = "Process end time. Value must be a DateTime, supports comparisons.")]
        public string EndTime { get; set; }

        [Option("just-one", Default = false, HelpText = "Only output one process")]
        public bool JustOne { get; set; }

        [Option('q', "quiet", Default = false, HelpText = "Show only Pid")]
        public bool Quiet { get; set; }

        [Usage]
        public static IEnumerable<Example> Examples => new List<Example>()
        {
            new Example("Find processes named 'codespaces'", new FindProc { Command = "codespaces" }),
            new Example("Find processes named 'codespaces' which use over 10% memory", new FindProc { Command = "codespaces", MemMax = "'>10'" }),
            new Example("Find processes where CPU usage passes 50%", new FindProc { CpuMax = "'>50'" }),
            new Example("Find processes where CPU usage passes 50% and is never lower than 10%", new FindProc { CpuMax = "'>50'", CpuMin = "'>10'" }),
            new Example("Find processes started at or after '10/12/2020 01:23:45'", new FindProc { StartTime = "'>=10/12/2020 01:23:45'" }),
        };

        public int Execute(TextWriter stdout, TextWriter stderr)
        {
            var logs = GetLogs(stdout, stderr);

            var allProcs = new Dictionary<int, ProcessFilterInfo>();

            foreach (var log in logs)
            {
                foreach (var proc in log.Processes)
                {
                    if (!allProcs.TryGetValue(proc.Pid, out var info))
                    {
                        allProcs[proc.Pid] = new ProcessFilterInfo()
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
                        info.MaxCpu = Math.Max(info.MaxCpu, proc.CpuUsage);
                        info.MinCpu = Math.Min(info.MinCpu, proc.CpuUsage);

                        info.MaxMem = Math.Max(info.MaxMem, proc.MemUsage);
                        info.MinMem = Math.Min(info.MinMem, proc.MemUsage);

                        info.EndTime = log.Time;
                    }
                }
            }

            var filteredProcs = Filter(allProcs);

            Output(filteredProcs, stdout);            

            return 0;
        }

        private IEnumerable<(int Pid, ProcessFilterInfo Info)> Filter(Dictionary<int, ProcessFilterInfo> procs)
        {
            foreach (var (pid, info) in procs)
            {
                if ((Pid == null || Pid == pid) &&
                    StringMatches(Command, info.Command) &&
                    StringMatches(User, info.User) &&
                    ComparableMatches(CpuMin, info.MinCpu, double.Parse) &&
                    ComparableMatches(CpuMax, info.MaxCpu, double.Parse) &&
                    ComparableMatches(MemMin, info.MinMem, double.Parse) &&
                    ComparableMatches(MemMax, info.MaxMem, double.Parse) &&
                    ComparableMatches(StartTime, info.StartTime, DateTime.Parse) &&
                    ComparableMatches(EndTime, info.EndTime, DateTime.Parse))
                {
                    yield return (pid, info);
                }

            }
        }

        private static bool StringMatches(string filter, string value)
        {
            return string.IsNullOrWhiteSpace(filter) || string.Equals(filter, value, StringComparison.OrdinalIgnoreCase);
        }

        private static bool ComparableMatches<T>(string filter, T value, Func<string, T> parse)
            where T : IComparable<T>
        {
            if (filter == null)
            {
                return true; // No filter specified
            }

            if (filter.StartsWith(">="))
            {
                var filterValue = parse(filter.Substring(2));
                return value.CompareTo(filterValue) >= 0;
            }

            if (filter.StartsWith("<="))
            {
                var filterValue = parse(filter.Substring(2));
                return value.CompareTo(filterValue) <= 0;
            }

            if (filter.StartsWith(">"))
            {
                var filterValue = parse(filter.Substring(1));
                return value.CompareTo(filterValue) > 0;
            }

            if (filter.StartsWith("<"))
            {
                var filterValue = parse(filter.Substring(1));
                return value.CompareTo(filterValue) < 0;
            }

            return value.CompareTo(parse(filter)) == 0;
        }

        private void Output(IEnumerable<(int Pid, ProcessFilterInfo Info)> procs, TextWriter stdout)
        {
            if (Quiet)
            {
                foreach (var (pid, _) in procs.OrderBy(m => m.Pid))
                {
                    stdout.WriteLine(pid);
                }
            }
            else
            {
                stdout.WriteLine(string.Join("\t", "PID", "COMMAND", "USER", "START", "END", "MIN CPU", "MAX CPU", "MIN MEM", "MAX MEM"));

                foreach (var (pid, info) in procs.OrderBy(m => m.Pid))
                {
                    stdout.WriteLine(string.Join("\t", pid, info.Command, info.User, info.StartTime, info.EndTime, info.MinCpu, info.MaxCpu, info.MinMem, info.MaxMem));

                    if (JustOne)
                    {
                        break;
                    }
                }
            }
        }

        private class ProcessFilterInfo
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
