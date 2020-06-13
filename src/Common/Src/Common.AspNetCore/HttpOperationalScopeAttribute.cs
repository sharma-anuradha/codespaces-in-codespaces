// <copyright file="HttpOperationalScopeAttribute.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore
{
    /// <summary>
    /// Http Scope Attribute.
    /// </summary>
    public class HttpOperationalScopeAttribute : Attribute, IAsyncActionFilter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpOperationalScopeAttribute"/> class.
        /// </summary>
        /// <param name="name">Base name of the log message.</param>
        public HttpOperationalScopeAttribute(string name)
        {
            Name = name;
        }

        private string Name { get; }

        /// <inheritdoc/>
        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            // Setup logging
            var duration = Stopwatch.StartNew();
            var logger = context.HttpContext.GetLogger();

            // Setup the logger as an arg for convenence
            context.ActionArguments["logger"] = logger;

            // Execute the delegate action
            var resultContext = await next();

            // Process result object
            logger.FluentAddValue("HttpResponseResultType", resultContext.Result?.GetType().Name);

            if (resultContext.Result is ObjectResult objectResult)
            {
                logger.FluentAddValue("HttpResponseResultObjectType", objectResult.Value?.GetType().Name);
            }

            var statusCode = resultContext.HttpContext.Response.StatusCode;
            if (resultContext.Result is IStatusCodeActionResult statusCodeActionResult
                && statusCodeActionResult?.StatusCode != null)
            {
                statusCode = statusCodeActionResult.StatusCode.Value;
                logger.FluentAddValue("HttpResponseResultStatusCode", statusCode);
            }

            // Getting logging name
            var loggingName = BuildLogName(context, Name);

            // Deal with exceptions
            if (resultContext.Exception == null)
            {
                if (statusCode >= 500)
                {
                    logger.FluentAddDuration(duration).LogError($"{loggingName}_error");
                }
                else
                {
                    logger.FluentAddDuration(duration).LogInfo($"{loggingName}_complete");
                }
            }
            else
            {
                logger.FluentAddDuration(duration).LogException($"{loggingName}_error", resultContext.Exception);
            }
        }

        private static string BuildLogName(ActionExecutingContext context, string name)
        {
            var controllerType = context.Controller.GetType();
            var loggingBaseNameAttribute = controllerType.GetCustomAttributes(typeof(LoggingBaseNameAttribute), inherit: true).OfType<LoggingBaseNameAttribute>().FirstOrDefault();
            var loggingBaseName = loggingBaseNameAttribute?.LoggingBaseName?.ToLowerInvariant();

            var loggingName = name;
            if (!string.IsNullOrEmpty(loggingBaseName))
            {
                loggingName = $"{loggingBaseName}_{loggingName}";
            }

            return loggingName;
        }
    }
}
