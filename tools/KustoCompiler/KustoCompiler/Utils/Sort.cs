using Microsoft.VsCloudKernel.Services.KustoCompiler.Runner;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VsCloudKernel.Services.KustoCompiler.Utils
{
    public static class Sort
    {
        public static List<T> TopologicalSort<T>(Dictionary<string, T> nameMap) where T : KustoQueryBase
        {
            List<Tuple<T, T>> edges = new List<Tuple<T, T>>();
            foreach (var item in nameMap.Values)
            {
                foreach (var dep in item.DependentFunction)
                {
                    var target = nameMap[dep];
                    edges.Add(new Tuple<T, T>(item, target));
                }
            }

            var result = new List<T>();
            var sink = new HashSet<T>(nameMap.Values.Where(n => edges.All(e => e.Item2.FunctionName != n.FunctionName)));
            while (sink.Any())
            {
                var n = sink.First();
                sink.Remove(n);

                result.Add(n);
                foreach (var e in edges.Where(e => e.Item1.FunctionName == n.FunctionName).ToList())
                {
                    var m = e.Item2;
                    edges.Remove(e);
                    if (edges.All(me => me.Item2.FunctionName != m.FunctionName))
                    {
                        sink.Add(m);
                    }
                }
            }

            if (edges.Any())
            {
                Console.WriteLine("Has loop");
                return null;
            }
            else
            {
                result.Reverse();
                return result;
            }
        }
    }
}
