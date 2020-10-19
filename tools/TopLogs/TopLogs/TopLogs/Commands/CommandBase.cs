using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VsSaaS.Tools.TopLogs.Models;

namespace Microsoft.VsSaaS.Tools.TopLogs.Commands
{
    public abstract class CommandBase
    {
        [Option('i', "input", Default = null, HelpText = "The input top_logs.txt file. If not provided, stdin is read instead")]
        public string InputFile { get; set; }
        
        [Option('C', Default = 0, HelpText = "Handling of parse errors:\n\t0 = exit\n\t1 = report and continue\n\t2 = small report and continue\n\tElse = ignore and continue")]
        public int IgnoreParseErrors { get; set; }

        protected List<TopLog> GetLogs(TextWriter stdout, TextWriter stderr)
        {
            string text;

            if (!string.IsNullOrEmpty(InputFile))
            {
                text = File.ReadAllText(InputFile);
            }
            else
            {
                text = Console.In.ReadToEnd();
            }

            var entryStrs = text.Split("---");

            return entryStrs
                .Select(e => e.Trim())
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select((entry) =>
                {
                    try
                    {
                        return TopLog.Parse(entry);
                    }
                    catch (Exception e)
                    {
                        if (IgnoreParseErrors == 0)
                        {
                            throw;
                        }

                        HandleIgnoreErrorSetting(e, stderr);
                        return null;
                    }
                })
                .Where(log => log != null)
                .ToList();
        }

        private void HandleIgnoreErrorSetting(Exception e, TextWriter stderr)
        {
            string shortMessage, longMessage;

            if (e is TopLog.ParseException pe)
            {
                shortMessage = pe.ShortMessage;
                longMessage = pe.LongMessage;
            }
            else
            {
                shortMessage = e.Message;
                longMessage = e.ToString();
            }

            if (IgnoreParseErrors == 1)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                stderr.WriteLine(longMessage);
                Console.ResetColor();
            }

            if (IgnoreParseErrors == 2)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                stderr.WriteLine(shortMessage);
                Console.ResetColor();
            }
        }
    }
}
