using Microsoft.VsCloudKernel.Services.KustoCompiler.Processor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.Services.KustoCompiler.Runner
{
    public class Inline
    {
        private const string WildCardFilter = "*.ksf";

        private PreProcessor preProcessor = new PreProcessor();

        public Task ExecuteInlineOutputAsync(string inputFile, string basePath)
        {
            var output = preProcessor.ProcessForInline(inputFile, basePath);
            Console.Write(output.Content);
            return Task.CompletedTask;
        }
    }
}
