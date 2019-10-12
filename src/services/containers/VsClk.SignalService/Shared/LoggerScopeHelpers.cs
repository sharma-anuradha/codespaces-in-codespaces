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
        public const string MethodScope = "Method";
        public const string MethodPerfScope = "MethodPerf";

        public static IDisposable BeginScope(this ILogger logger, params (string, object)[] scopes )
        {
            var loggerScope = new JObject();
            foreach(var scope in scopes)
            {
                loggerScope[scope.Item1] = scope.Item2 != null ? JToken.FromObject(scope.Item2) : JValue.CreateNull();
            }

            return logger.BeginScope(loggerScope);
        }

        public static IDisposable BeginSingleScope(this ILogger logger, (string, object) scope)
        {
            return BeginScope(logger, new (string, object)[] { scope });
        }

        public static IDisposable BeginMethodScope(this ILogger logger, string methodScope)
        {
            return logger.BeginSingleScope((MethodScope, methodScope));
        }
    }
}
