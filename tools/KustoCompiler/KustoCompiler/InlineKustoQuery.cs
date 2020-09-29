using Microsoft.VsCloudKernel.Services.KustoCompiler.Runner;
using System;
using System.IO;
using System.Threading.Tasks;

namespace KustoCompiler
{
    internal class InlineKustoQuery
    {
        public async Task ExecuteAsync(string input, string basePath)
        {
            var inline = new Inline();

            if (File.Exists(input))
            {
                var inputFile = Path.GetFullPath(input);
                await inline.ExecuteInlineOutputAsync(inputFile, basePath);
            }
            else
            {
                throw new NotSupportedException("Folder not supported.");
            }
        }
    }
}