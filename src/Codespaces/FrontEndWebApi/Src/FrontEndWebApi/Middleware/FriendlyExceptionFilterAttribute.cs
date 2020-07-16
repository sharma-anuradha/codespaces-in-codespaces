// <copyright file="FriendlyExceptionFilterAttribute.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.VsSaaS.Common.Identity;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Middleware
{
    /// <summary>
    /// Maps <see cref="ExceptionContext"/> to <see cref="StatusCodeResult"/>.
    /// </summary>
    /// <remarks>
    /// TODO: This should be consolidated with the VSSAAS-SDK's UnhandledExceptionReporter
    ///       but this will do for the time being.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class FriendlyExceptionFilterAttribute : ExceptionFilterAttribute
    {
        /// <inheritdoc/>
        public override void OnException(ExceptionContext context)
        {
            switch (context.Exception)
            {
                case CodedException codedException:
                    context.Result = codedException switch
                    {
                        ConflictException ex => new ConflictObjectResult(ex.MessageCode),
                        EntityNotFoundException ex => new NotFoundObjectResult(ex.MessageCode),
                        ForbiddenException ex => new ObjectResult(ex.MessageCode) { StatusCode = StatusCodes.Status403Forbidden },
                        ProcessingFailedException ex => new ObjectResult(ex.MessageCode) { StatusCode = StatusCodes.Status500InternalServerError },
                        UnavailableException ex => new ObjectResult(ex.MessageCode) { StatusCode = StatusCodes.Status503ServiceUnavailable },
                        _ => new ObjectResult(codedException.MessageCode) { StatusCode = StatusCodes.Status503ServiceUnavailable },
                    };
                    break;

                case RedirectToLocationException redirectToLocationException:
                    var builder = new UriBuilder()
                    {
                        Host = redirectToLocationException.OwningStamp,
                        Path = context.HttpContext.Request.Path,
                        Query = context.HttpContext.Request.QueryString.Value,
                        Scheme = Uri.UriSchemeHttps,
                    };
                    context.Result = new RedirectResult(builder.ToString(), permanent: false, preserveMethod: true);
                    break;

                case ArgumentException _:
                    context.Result = CreateBadRequestResult(context);
                    break;

                case ValidationException _:
                    context.Result = CreateBadRequestResult(context);
                    break;

                case UnauthorizedAccessException _:
                    context.Result = new ForbidResult();
                    break;

                case IdentityValidationException _:
                    context.Result = new UnauthorizedResult();
                    break;
            }
        }

        private static IActionResult CreateBadRequestResult(ExceptionContext context)
        {
            if (string.IsNullOrEmpty(context.Exception.Message))
            {
                return new BadRequestResult();
            }
            else
            {
                return new BadRequestObjectResult(context.Exception.Message);
            }
        }
    }
}
