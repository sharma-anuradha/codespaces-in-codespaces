using Microsoft.VsCloudKernel.Services.KustoCompiler.Models;
using System;
using System.Collections.Generic;

namespace Microsoft.VsCloudKernel.Services.KustoCompiler.Analyzers
{
    public class LexicalAnalyzer
    {
        public List<Token> Analyze(string input)
        {
            var tokens = new List<Token>();

            var lines = input.Split(
                    new[] { "\r\n", "\r", "\n" },
                    StringSplitOptions.None
                );

            foreach (var line in lines)
            {
                var token = default(Token);
                if (!string.IsNullOrWhiteSpace(line) && line.TrimStart().StartsWith(IncludeFileToken.IncludeToken))
                {
                    token = IncludeFileToken.Extract(line);
                }
                else if (!string.IsNullOrWhiteSpace(line) && line.TrimStart().StartsWith(DefineToken.Token))
                {
                    token = DefineToken.Extract(line);
                }
                else if (!string.IsNullOrWhiteSpace(line) && line.TrimStart().StartsWith(UndefineToken.Token))
                {
                    token = UndefineToken.Extract(line);
                }
                else
                {
                    token = new LineToken()
                    {
                        Line = line
                    };
                }

                tokens.Add(token);
            }

            return tokens;
        }
    }
}
