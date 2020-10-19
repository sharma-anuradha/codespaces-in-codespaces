using System;
using System.Linq;

namespace Microsoft.VsSaaS.Tools.TopLogs.Models
{
    public class ProcessDetails
    {
        // See https://www.man7.org/linux/man-pages/man1/top.1.html "Fields/Columns" section for details

        public int Pid;
        public string User;
        public string Priority;
        public int NiceValue;
        public int VirtualMemorySize;
        public int ResidentMemorySize;
        public int SharedMemorySize;
        public string Status;
        public double CpuUsage;
        public double MemUsage;
        public TimeSpan CpuTime;
        public string Command;

        public string[] Raw;

        private ProcessDetails()
        {
        }

        private static void AssertExpectedHeader(string[] header)
        {
            // Too lazy to dynamically parse this so just confirm that the order is correct

            var headerStr = string.Join(",", header);

            if (headerStr != "PID,USER,PR,NI,VIRT,RES,SHR,S,%CPU,%MEM,TIME+,COMMAND")
            {
                throw new FormatException("Cannot parse process details, header in unexpected format");
            }
        }

        public static ProcessDetails Parse(string[] header, string line)
        {
            AssertExpectedHeader(header);

            var columns = line.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();

            return new ProcessDetails
            {
                Pid = int.Parse(columns[0]),
                User = columns[1],
                Priority = columns[2],
                NiceValue = int.Parse(columns[3]),
                VirtualMemorySize = int.Parse(columns[4]),
                ResidentMemorySize = int.Parse(columns[5]),
                SharedMemorySize = int.Parse(columns[6]),
                Status = columns[7],
                CpuUsage = double.Parse(columns[8]),
                MemUsage = double.Parse(columns[9]),
                CpuTime = TimeSpan.ParseExact(columns[10], @"m\:ss\.ff", null),
                Command = columns[11],

                Raw = columns,
            };
        }
    }
}
