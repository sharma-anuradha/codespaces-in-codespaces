using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VsCloudKernel.Services.KustoCompiler.Models
{
    public class OverrideFunctionToken : Token
    {
        public const string OverrideFunctionVerb = "#overrideFunction";

        public string File
        {
            get;
            set;
        }

        public static OverrideFunctionToken Extract(string line)
        {
            line = line.TrimStart();
            line = line.Remove(0, OverrideFunctionVerb.Length);
            line = line.Remove(0, 2); // Remove bracket and quotes. (" 
            var endingQuoteAt = line.IndexOf('"'); // Find ending quote.
            line = line.Remove(endingQuoteAt);

            return new OverrideFunctionToken()
            {
                File = line,
            };
        }
    }
}
