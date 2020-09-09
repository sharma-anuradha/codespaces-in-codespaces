using Kusto.Language;
using Kusto.Language.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Microsoft.VsCloudKernel.Services.KustoCompiler.Runner
{
    [DebuggerDisplay("{FunctionName,nq}")]
    public class CslFile
    {
        public string FileName
        {
            get;
            set;
        }

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

        public static CslFile Create(string file)
        {
            var content = File.ReadAllText(file);
            var parsed = KustoCode.Parse(content);
            var analyzed = KustoCode.ParseAndAnalyze(content);

            var globalFunctions = parsed.Globals.Functions.Select(x => x.Name);
            var globalAggregates = parsed.Globals.Aggregates.Select(x => x.Name);

            var functions = parsed.Syntax.GetDescendants<FunctionCallExpression>().Where(x => !(globalFunctions.Contains(x.Name.SimpleName) || globalAggregates.Contains(x.Name.SimpleName)));

            return new CslFile()
            {
                FileName = file,
                FunctionName = Path.GetFileNameWithoutExtension(file), // Should get this from syntax tree?
                Content = content,
                DependentFunction = functions.Select(x => x.Name.SimpleName).ToHashSet(),
                ParsedCode = parsed,
            };
        }
    }
}
