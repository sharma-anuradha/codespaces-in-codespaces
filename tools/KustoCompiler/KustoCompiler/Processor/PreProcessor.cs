using Microsoft.VsCloudKernel.Services.KustoCompiler.Analyzers;
using Microsoft.VsCloudKernel.Services.KustoCompiler.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.VsCloudKernel.Services.KustoCompiler.Processor
{
    public class KustoSourceFileLexicalInfo
    {
        public string File
        {
            get;
            set;
        }

        public List<Token> Tokens
        {
            get;
            set;
        }
    }

    public class PreProcessor
    {
        private readonly LexicalAnalyzer lexicalAnalyzer = new LexicalAnalyzer();

        private readonly Dictionary<string, KustoSourceFileLexicalInfo> lexMap = new Dictionary<string, KustoSourceFileLexicalInfo>();

        public string Process(string inputFile)
        {
            Console.WriteLine($"  Processing: {inputFile}");

            var processQueue = new Queue<string>();
            processQueue.Enqueue(inputFile);

            while (processQueue.Count > 0)
            {
                var fileToProcess = processQueue.Dequeue();

                if (lexMap.ContainsKey(fileToProcess))
                {
                    continue;
                }

                Console.WriteLine($"  Including: {fileToProcess}");

                var input = File.ReadAllText(fileToProcess);
                var tokens = lexicalAnalyzer.Analyze(input);

                var lexInfo = new KustoSourceFileLexicalInfo()
                {
                    File = input,
                    Tokens = tokens,
                };

                lexMap[fileToProcess] = lexInfo;

                foreach (var token in tokens)
                {
                    if (token is IncludeFileToken includeFileToken)
                    {
                        var absolutePath = GetAbsolutePathBasedOn(fileToProcess, includeFileToken.File);
                        processQueue.Enqueue(absolutePath);
                    }
                }
            }

            var includedSet = new List<string>();
            return GenerateFor(inputFile, inputFile, includedSet);
        }

        private static string GetAbsolutePathBasedOn(string rootedFile, string target)
        {
            return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(rootedFile), target));
        }

        private string GenerateFor(string sourceFile, string includedFile, List<string> includedSet)
        {
            var absolutePath = GetAbsolutePathBasedOn(sourceFile, includedFile);

            if (includedSet.Contains(absolutePath))
            {
                return default;
            }

            var final = new StringBuilder();
            includedSet.Add(absolutePath);

            var lexInfo = lexMap[absolutePath];
            foreach (var token in lexInfo.Tokens)
            {
                if (token is LineToken lineToken)
                {
                    if (!string.IsNullOrWhiteSpace(lineToken.Line))
                    {
                        final.AppendLine(lineToken.Line);
                    }
                }
                else if (token is IncludeFileToken includeFileToken)
                {
                    var includedFileContent = GenerateFor(absolutePath, includeFileToken.File, includedSet);
                    if (includedFileContent != default)
                    {
                        final.AppendLine(includedFileContent.TrimEnd());
                    }
                }
                else
                {
                    throw new InvalidOperationException("Unknown token.");
                }
            }

            return final.ToString();
        }
    }
}
