using System;
using CommandLine;
using Microsoft.VsSaaS.Tools.TopLogs.Commands;

namespace Microsoft.VsSaaS.Tools.TopLogs
{
    public class Program
    {
        public static int Main(string[] args)
        {
            int exitCode = 0;

            try
            { 
                Parser.Default.ParseArguments(
                    args,
                    typeof(ShowTable),
                    typeof(FindProc),
                    typeof(ProcDetails))
                    .WithParsed<ShowTable>(command => command.Execute(Console.Out, Console.Error))
                    .WithParsed<FindProc>(command => command.Execute(Console.Out, Console.Error))
                    .WithParsed<ProcDetails>(command => command.Execute(Console.Out, Console.Error))
                    .WithNotParsed(errs => { exitCode = 1; });
            }
            catch (Exception ex)
            {
                PrintException(ex);
                exitCode = 1;
            }
            return exitCode;
        }

        private static void PrintException(Exception ex)
        {
            if (ex != null)
            {
                if (ex is AggregateException aggregate)
                {
                    foreach (var e in aggregate.InnerExceptions)
                    {
                        PrintException(e);
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine($"error: {ex.ToString()}");
                    Console.ResetColor();
                    PrintException(ex.InnerException);
                }
            }
        }
    }
}
