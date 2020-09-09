using System;

namespace Microsoft.VsCloudKernel.Services.KustoCompiler.Models
{
    public class FunctionToken : Token
    {
        public const string FunctionVerb = "#function";

        public string DocumentString
        {
            get;
            set;
        }

        public static FunctionToken Extract(string line)
        {
            line = line.Trim();
            line = line.Remove(0, FunctionVerb.Length);
            line = line[1..^1]; // Remove brackets ( )
            line = line.Trim();
            line = line[1..^1]; // Remove quotes

            var doc = line;

            return new FunctionToken()
            {
                DocumentString = doc,
            };
        }
    }
}
