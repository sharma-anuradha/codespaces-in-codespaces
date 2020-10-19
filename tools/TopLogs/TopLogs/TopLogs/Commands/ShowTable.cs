using CommandLine;
using System.IO;
using System.Linq;

namespace Microsoft.VsSaaS.Tools.TopLogs.Commands
{
    [Verb("show-table", HelpText = "Output log details as a TSV")]
    public class ShowTable : CommandBase
    {
        public int Execute(TextWriter stdout, TextWriter stderr)
        {
            var logs = GetLogs(stdout, stderr);

            var first = logs.First();
            var startTime = first.Time;

            stdout.WriteLine(string.Join("\t", "TIME", "MS", "USERS", "TASKS", "%CPU", "USER CPU", "SYSTEM CPU", "%MEM", "USED MEM", "FREE MEM", "AVAIL MEM"));
            foreach (var entry in logs)
            {
                var msSinceStart = (int)(entry.Time - startTime).TotalMilliseconds;

                stdout.WriteLine(string.Join("\t", entry.Time, msSinceStart, entry.Users, entry.RunningTasks, entry.PercentCpu, entry.UserCpu, entry.SystemCpu, entry.PercentMem, entry.UsedMem, entry.FreeMem, entry.AvailMem));
            }

            return 0;
        }        
    }
}
