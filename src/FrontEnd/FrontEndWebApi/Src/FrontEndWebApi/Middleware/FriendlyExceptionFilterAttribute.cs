// <copyright file="FriendlyExceptionFilterAttribute.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Middleware
{
    /// <summary>
    /// Maps <see cref="ExceptionContext"/> to <see cref="StatusCodeResult"/>.
    /// </summary>
    /// <remarks>
    /// TODO: This should be consolidated with the VSSAAS-SDK's UnhandledExceptionReporter
    ///       but this will do for the time being.
    /// </remarks>
    public class FriendlyExceptionFilterAttribute : ExceptionFilterAttribute
    {
        /// <inheritdoc/>
        public override void OnException(ExceptionContext context)
        {
            if (context.Exception is ValidationException)
            {
                if (string.IsNullOrEmpty(context.Exception.Message))
                {
                    context.Result = new BadRequestResult();
                }
                else
                {
                    context.Result = new BadRequestObjectResult(context.Exception.Message);
                }
            }
            else if (context.Exception is UnauthorizedAccessException)
            {
                context.Result = new StatusCodeResult((int)HttpStatusCode.Unauthorized);
            }
        }
    }
}
