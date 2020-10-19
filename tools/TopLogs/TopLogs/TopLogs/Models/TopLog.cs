using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.VsSaaS.Tools.TopLogs.Models
{
    /*
        Sample log entry:

        13:16:43.609912208
        top - 13:16:43 up 11 min,  1 user,  load average: 1.65, 0.63, 0.34
        Tasks: 152 total,   2 running,  85 sleeping,   0 stopped,   0 zombie
        %Cpu(s):  3.7 us,  2.5 sy,  0.4 ni, 82.9 id, 10.2 wa,  0.0 hi,  0.2 si,  0.0 st
        KiB Mem :  4030860 total,   135256 free,   842028 used,  3053576 buff/cache
        KiB Swap:        0 total,        0 free,        0 used.  2936080 avail Mem 

          PID USER      PR  NI    VIRT    RES    SHR S  %CPU %MEM     TIME+ COMMAND
         4073 root      20   0       0      0      0 R  31.2  0.0   0:05.34 cifsd
         4127 root      20   0  963612  97732  44360 S  31.2  2.4   0:04.40 dockerd
           89 root      20   0       0      0      0 S   6.2  0.0   0:00.02 kswapd0
         4109 root       0 -20       0      0      0 D   6.2  0.0   0:02.91 loop0
            1 root      20   0   78152   9292   6744 S   0.0  0.2   0:03.14 systemd
            2 root      20   0       0      0      0 S   0.0  0.0   0:00.00 kthreadd
            3 root       0 -20       0      0      0 I   0.0  0.0   0:00.00 rcu_gp
            4 root       0 -20       0      0      0 I   0.0  0.0   0:00.00 rcu_par_gp
            5 root      20   0       0      0      0 I   0.0  0.0   0:00.01 kworker/0:+
            6 root       0 -20       0      0      0 I   0.0  0.0   0:00.00 kworker/0:+
    
    */

    public class TopLog
    {
        // Raw data

        public DateTime Time;
        public int Users;
        public List<double> LoadAvg; // 1 min, 5 min, 15 min
        public IReadOnlyDictionary<string, int> Tasks;
        public IReadOnlyDictionary<string, double> CpuUsage;
        public IReadOnlyDictionary<string, int> MemUsage;
        public IReadOnlyDictionary<string, int> SwapUsage;
        public int AvailMem;
        public IReadOnlyDictionary<string, List<string>> ProcessesRaw;
        public IReadOnlyList<ProcessDetails> Processes;

        // Computed data

        public int TotalTasks => Tasks["total"];
        public int RunningTasks => Tasks["running"];

        public double UserCpu => CpuUsage["us"];
        public double SystemCpu => CpuUsage["sy"];
        public double PercentCpu => ProcessesRaw["%CPU"].Select(double.Parse).Sum();

        public int UsedMem => MemUsage["used"];
        public int FreeMem => MemUsage["free"];
        public double PercentMem => ProcessesRaw["%MEM"].Select(double.Parse).Sum();

        private TopLog()
        {
        }

        public static TopLog Parse(string entry)
        {
            var lines = entry.Split('\n').Select(l => l.Trim()).ToList();
            var i = 0;

            string timeStr = default; // Used for exceptions

            try
            {
                var line = lines[i++];
                timeStr = line;
                var time = DateTime.Parse(timeStr);

                var match = Regex.Match(lines[i++], @"top\s+-\s+([\d:]+)\s+up\s+[^,]+,\s+(\d+)\s+user,\s+load\s+average:\s+(.+)$");
                if (!match.Success)
                {
                    throw new FormatException("Failed to parse top line");
                }

                var topTime = match.Groups[1].Value;
                var users = int.Parse(match.Groups[2].Value);
                var loadAverages = ParseList(match.Groups[3].Value, double.Parse);

                match = Regex.Match(lines[i++], @"Tasks\s*:(.+)");
                if (!match.Success)
                {
                    throw new FormatException("Failed to parse Tasks line");
                }

                var tasks = ParseMap(match.Groups[1].Value, int.Parse);

                match = Regex.Match(lines[i++], @"%Cpu\(s\)\s*:(.+)");
                if (!match.Success)
                {
                    throw new FormatException("Failed to parse %Cpu line");
                }

                var cpu = ParseMap(match.Groups[1].Value, double.Parse);

                match = Regex.Match(lines[i++], @"KiB\s+Mem\s*:(.+)");
                if (!match.Success)
                {
                    throw new FormatException("Failed to parse Mem line");
                }

                var mem = ParseMap(match.Groups[1].Value, int.Parse);

                match = Regex.Match(lines[i++], @"KiB\s+Swap\s*:(.+)\.\s+(\d+)\s+avail\s+Mem");
                if (!match.Success)
                {
                    throw new FormatException("Failed to parse Swap line");
                }

                var swap = ParseMap(match.Groups[1].Value, int.Parse);
                var availMem = int.Parse(match.Groups[2].Value);

                // Blank line
                i++;

                var processesHeader = lines[i++].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(h => h.Trim()).ToArray();

                var processes = new List<ProcessDetails>();

                while (i < lines.Count)
                {
                    line = lines[i++]?.Trim();

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    processes.Add(ProcessDetails.Parse(processesHeader, line));
                }

                var processMap = Enumerable.Range(0, processesHeader.Length).ToDictionary(col => processesHeader[col], col => processes.Select(x => x.Raw[col]).ToList());

                return new TopLog
                {
                    Time = time,
                    Users = users,
                    LoadAvg = loadAverages,
                    Tasks = tasks,
                    CpuUsage = cpu,
                    MemUsage = mem,
                    SwapUsage = swap,
                    AvailMem = availMem,
                    ProcessesRaw = processMap,
                    Processes = processes,
                };
            }
            catch (Exception ex)
            {
                var line = i > 0 && i < lines.Count ? lines[i - 1] : "Unknown";

                throw new ParseException(timeStr, line, entry, ex);
            }
        }

        private static List<string> ParseList(string listStr)
        {
            return listStr
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToList();
        }

        private static List<T> ParseList<T>(string listStr, Func<string, T> convert)
        {
            return ParseList(listStr)
                .Select(convert)
                .ToList();
        }

        private static Dictionary<string, string> ParseMap(string mapStr)
        {
            return ParseList(mapStr)
                .Select(x => x.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .ToDictionary(pair => pair[1], pair => pair[0]);
        }

        private static Dictionary<string, T> ParseMap<T>(string mapStr, Func<string, T> convert)
        {
            return ParseMap(mapStr)
                .ToDictionary(kvp => kvp.Key, kvp => convert(kvp.Value));
        }

        public class ParseException : Exception
        {
            public string TimeStamp { get; }

            public string Line { get; }

            public string FullText { get; }

            public string ShortMessage => $"Parse error at timestamp {TimeStamp} for line '{Line}'";

            public string LongMessage => this.ToString();

            public ParseException(string timestamp, string line, string fullText, Exception exception)
                : base($"Failed to parse TopLog:\n\nSuspected line:\n{line}\n\nFull log text:\n{fullText}", exception)
            {
                TimeStamp = timestamp;
                Line = line;
                FullText = fullText;
            }
        }
    }
}
