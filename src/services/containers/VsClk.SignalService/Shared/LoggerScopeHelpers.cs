using System;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsCloudKernel.SignalService.Common
{
    /// <summary>
    /// Helper class to allow pushing a scope usign a key value pair
    /// </summary>
    internal static class LoggerScopeHelpers
    {
        public static IDisposable BeginScope(this ILogger logger, params (string, object)[] scopes )
        {
            var loggerScope = new JObject();
            foreach(var scope in scopes)
            {
                loggerScope[scope.Item1] = JToken.FromObject(scope.Item2);
            }

            return logger.BeginScope(loggerScope);
        }
    }
}
