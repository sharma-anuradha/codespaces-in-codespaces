using Microsoft.VsCloudKernel.Services.KustoCompiler.Runner;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.Services.KustoCompiler
{
    public class RunKustoQuery
    {
        public async Task ExecuteAsync(string input)
        {
            var upload = new Upload();

            if (Directory.Exists(input))
            {
                var inputDirectory = Path.GetFullPath(input);
                await upload.ExecuteAllControlQueriesAsync(inputDirectory);
            }
            else if (File.Exists(input))
            {
                var inputFile = Path.GetFullPath(input);
                await upload.ExecuteAllControlQueriesAsync(inputFile);
            }
        }
    }
}
