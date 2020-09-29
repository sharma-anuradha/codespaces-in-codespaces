using Kusto.Language;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VsCloudKernel.Services.KustoCompiler.Runner
{
    public abstract class KustoQueryBase
    {
        public string FunctionName
        {
            get;
            set;
        }

        public string Content
        {
            get;
            set;
        }

        public KustoCode ParsedCode
        {
            get;
            set;
        }

        public HashSet<string> DependentFunction
        {
            get;
            set;
        }
    }
}
