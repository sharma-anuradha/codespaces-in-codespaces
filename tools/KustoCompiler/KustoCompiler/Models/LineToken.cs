using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VsCloudKernel.Services.KustoCompiler.Models
{
    public class LineToken : Token
    {
        public string Line
        {
            get;
            set;
        }
    }
}
