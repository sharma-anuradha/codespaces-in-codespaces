// <copyright file="HttpContextExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore.Extensions
{
    /// <summary>
    /// Http Context Extensions.
    /// </summary>
    public static class HttpContextExtensions
    {
        /// <summary>
        /// Wraps the given operation in a logging scope which will catch and log exceptions
        /// as well as the successful completeion of the operation.
        /// </summary>
        /// <typeparam name="T">Return type.</typeparam>
        /// <param name="httpContext">Target http context.</param>
        /// <param name="name">Base name of the operation scope.</param>
        /// <param name="callback">Callback that should be executed.</param>
        /// <param name="errCallback">Callback that should be executed if an exception occurs.</param>
        /// <param name="swallowException">Whether any exceptions should be swallowed.</param>
        /// <returns>Returns the task.</returns>
        public static Task<T> HttpScopeAsync<T>(
            this HttpContext httpContext,
            string name,
            Func<IDiagnosticsLogger, Task<T>> callback,
            Func<Exception, IDiagnosticsLogger, Task<T>> errCallback = default,
            bool swallowException = false)
        {
            return httpContext.GetLogger().OperationScopeAsync(
                name,
                (childLogger) => InnerCallbackAsync(callback, childLogger),
                (ex, childLogger) =>
                {
                    var result = errCallback(ex, childLogger);

                    childLogger.ProcessHttpScopeResult(result);

                    return result;
                },
                swallowException);
        }

        /// <summary>
        /// Wraps the given operation in a logging scope which will catch and log exceptions
        /// as well as the successful completeion of the operation.
        /// </summary>
        /// <typeparam name="T">Return type.</typeparam>
        /// <param name="httpContext">Target http context.</param>
        /// <param name="name">Base name of the operation scope.</param>
        /// <param name="callback">Callback that should be executed.</param>
        /// <param name="errCallback">Callback that should be executed if an exception occurs.</param>
        /// <returns>Returns the task.</returns>
        public static Task<T> HttpScopeWithCustomExceptionControlFlowAsync<T>(
            this HttpContext httpContext,
            string name,
            Func<IDiagnosticsLogger, Task<T>> callback,
            Func<Exception, IDiagnosticsLogger, (bool SwallowException, Task<T> Result)> errCallback)
        {
            return httpContext.GetLogger().OperationScopeWithCustomExceptionHandlingAsync(
                name,
                (childLogger) => InnerCallbackAsync(callback, childLogger),
                (ex, childLogger) =>
                {
                    var result = errCallback(ex, childLogger);

                    childLogger.ProcessHttpScopeResult(result.Result);

                    return result;
                });
        }

        /// <summary>
        /// Given a result from an action process the result object.
        /// </summary>
        /// <typeparam name="T">Return type.</typeparam>
        /// <param name="logger">Target logger.</param>
        /// <param name="result">Target result.</param>
        internal static void ProcessHttpScopeResult<T>(this IDiagnosticsLogger logger, T result)
        {
            logger.FluentAddValue("HttpResponseResultType", result?.GetType().Name);

            if (result is ObjectResult objectResult)
            {
                logger.FluentAddValue("HttpResponseResultObjectType", objectResult.Value?.GetType().Name);
            }

            if (result is IStatusCodeActionResult statusCodeActionResult)
            {
                logger.FluentAddValue("HttpResponseResultStatusCode", statusCodeActionResult.StatusCode);
            }
        }

        private static async Task<T> InnerCallbackAsync<T>(
            Func<IDiagnosticsLogger, Task<T>> callback, IDiagnosticsLogger logger)
        {
            var result = await callback(logger);

            logger.ProcessHttpScopeResult(result);

            return result;
        }
    }
}
