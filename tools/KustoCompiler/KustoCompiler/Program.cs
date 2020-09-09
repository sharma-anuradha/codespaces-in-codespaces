using Kusto.Data;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VsCloudKernel.Services.KustoCompiler;
using System;
using System.Globalization;
using System.Net;

namespace KustoCompiler
{
    class Program : CommandLineApplication
    {
        static void Main(string[] args)
        {
            var app = new Program
            {
                Name = "KustoCompiler",
                Description = "Kusto Source Compiler"
            };

            app.HelpOption("-?|-h|--help");
            app.Command("compile", (command) =>
            {
                ExecuteCompileCommand(command);
            });

            app.Command("runFunctionUpdate", (command) =>
            {
                ExecuteFunctionUpdateCommand(command);
            });


            try
            {
                app.Execute(args);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static void ExecuteCompileCommand(CommandLineApplication command)
        {
            command.Description = "This command creates kusto query files (.kql) from kusto source files (.ksf)";
            command.HelpOption("-?|-h|--help");

            var inputOption = command.Option("-i|--input", "Input file or folder.", CommandOptionType.SingleValue);
            var outputOption = command.Option("-o|--output", "Output file or folder.", CommandOptionType.SingleValue);
            var filterOption = command.Option("-f|--filter", "Wild card filter for files when specifying a folder.", CommandOptionType.SingleValue);

            command.OnExecute(() =>
            {
                var input = inputOption.Value();
                var output = outputOption.Value();
                var filter = filterOption.HasValue() ? filterOption.Value() : "*.ksf";

                var compiler = new Compiler();
                compiler.Execute(input, output, filter);
                return 0;
            });
        }

        private static void ExecuteFunctionUpdateCommand(CommandLineApplication command)
        {
            command.Description = "This command executes function update queries (.csl)";
            command.HelpOption("-?|-h|--help");

            var inputOption = command.Option("-i|--input", "Input file or folder.", CommandOptionType.SingleValue);

            command.OnExecute(() =>
            {
                var input = inputOption.Value();

                var run = new RunKustoQuery();
                run.ExecuteAsync(input).Wait();
                return 0;
            });
        }
    }
}
