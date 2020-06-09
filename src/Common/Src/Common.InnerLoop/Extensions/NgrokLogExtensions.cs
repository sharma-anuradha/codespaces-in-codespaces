// <copyright file="NgrokLogExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore.Extensions
{
    /// <summary>
    /// Extensions for managing Ngrok Logs.
    /// </summary>
    public static class NgrokLogExtensions
    {
        /// <summary>
        /// Parses Ngrok Log Data.
        /// </summary>
        /// <param name="input">Ngrok Log Input.</param>
        /// <returns>A dictionary log.</returns>
        public static Dictionary<string, string> ParseLogData(string input)
        {
            var result = new Dictionary<string, string>();
            var stream = new StringReader(input);
            int lastRead = 0;

            while (lastRead > -1)
            {
                // Read Key
                var keyBuilder = new StringBuilder();
                while (true)
                {
                    lastRead = stream.Read();
                    var c = (char)lastRead;
                    if (c == '=')
                    {
                        break;
                    }

                    keyBuilder.Append(c);
                }

                // Read Value
                var valueBuilder = new StringBuilder();
                lastRead = stream.Read();
                var firstValChar = (char)lastRead;
                bool quoteWrapped = false;
                if (firstValChar == '"')
                {
                    quoteWrapped = true;
                    lastRead = stream.Read();
                    valueBuilder.Append((char)lastRead);
                }
                else
                {
                    valueBuilder.Append(firstValChar);
                }

                while (true)
                {
                    lastRead = stream.Read();
                    if (lastRead == -1)
                    {
                        break;
                    }

                    var c = (char)lastRead;
                    if (quoteWrapped && c == '"')
                    {
                        lastRead = stream.Read();
                        break;
                    }

                    if (!quoteWrapped && c == ' ')
                    {
                        break;
                    }

                    valueBuilder.Append(c);
                }

                result.Add(keyBuilder.ToString(), valueBuilder.ToString());
            }

            return result;
        }

        /// <summary>
        /// Parses Ngrok Log Level.
        /// </summary>
        /// <param name="logLevelRaw">The raw Ngrok Log Level.</param>
        /// <returns>Ngrok Log Level.</returns>
        public static LogLevel ParseLogLevel(string logLevelRaw)
        {
            if (!string.IsNullOrWhiteSpace(logLevelRaw))
            {
                return LogLevel.Debug;
            }

            LogLevel logLevel;
            switch (logLevelRaw)
            {
                case "info":
                    logLevel = LogLevel.Information;
                    break;
                default:
                    var parseResult = Enum.TryParse<LogLevel>(logLevelRaw, out logLevel);
                    if (!parseResult)
                    {
                        logLevel = LogLevel.Debug;
                    }

                    break;
            }

            return logLevel;
        }

        /// <summary>
        /// Gets the Log Format String.
        /// </summary>
        /// <param name="logFormatData">The Log Format Data.</param>
        /// <returns>A string.</returns>
        public static string GetLogFormatString(Dictionary<string, string> logFormatData)
        {
            StringBuilder logFormatSB = new StringBuilder();
            foreach (var kvp in logFormatData)
            {
                logFormatSB.Append(kvp.Key);
                logFormatSB.Append(": {");
                logFormatSB.Append(kvp.Key);
                logFormatSB.Append("} | ");
            }

            var logFormatString = logFormatSB.ToString().TrimEnd(' ').TrimEnd('|').TrimEnd(' ');
            return logFormatString;
        }
    }
}
