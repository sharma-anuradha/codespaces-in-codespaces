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
    public class CslFile : KustoQueryBase
    {
        public string FileName
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

            var localFunctions = GetLocalFunctionNames(parsed);

            var ignoredNames = new HashSet<string>();
            ignoredNames.UnionWith(globalFunctions);
            ignoredNames.UnionWith(globalAggregates);
            ignoredNames.UnionWith(localFunctions);

            var functions = parsed.Syntax.GetDescendants<FunctionCallExpression>().Where(x => !ignoredNames.Contains(x.Name.SimpleName));

            return new CslFile()
            {
                FileName = file,
                FunctionName = Path.GetFileNameWithoutExtension(file), // Should get this from syntax tree?
                Content = content,
                DependentFunction = functions.Select(x => x.Name.SimpleName).ToHashSet(),
                ParsedCode = parsed,
            };
        }

        private static IEnumerable<string> GetLocalFunctionNames(KustoCode parsed)
        {
            // This is for functions defined inside this function file - the syntax of these is like:
            //
            //     let myLocalFunction=(arg1:string) {
            //         doSomething
            //     };
            //
            // Here we get the function declaration (which is like `(arg1: ... };` above) and then get the name
            // of its parent which is the name of the function.

            return parsed.Syntax
                .GetDescendants<FunctionDeclaration>()
                .Select(x => x.Parent)
                .OfType<LetStatement>().Select(x => x.Name.Name.SimpleName);
        }
    }
}
