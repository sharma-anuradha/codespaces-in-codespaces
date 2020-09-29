using Kusto.Language;
using Kusto.Language.Syntax;
using System;
using System.Linq;

namespace Microsoft.VsCloudKernel.Services.KustoCompiler.Runner
{
    public class KustoQueryBlob : KustoQueryBase
    {
        public static KustoQueryBlob Create(string name, string content)
        {
            var parsed = KustoCode.Parse(content);
            var analyzed = KustoCode.ParseAndAnalyze(content);

            var globalFunctions = parsed.Globals.Functions.Select(x => x.Name);
            var globalAggregates = parsed.Globals.Aggregates.Select(x => x.Name);

            var functions = parsed.Syntax.GetDescendants<FunctionCallExpression>().Where(x => !(globalFunctions.Contains(x.Name.SimpleName) || globalAggregates.Contains(x.Name.SimpleName)));

            return new KustoQueryBlob()
            {
                FunctionName = name,
                Content = content,
                DependentFunction = functions.Select(x => x.Name.SimpleName).ToHashSet(),
                ParsedCode = parsed,
            };
        }
    }
}
