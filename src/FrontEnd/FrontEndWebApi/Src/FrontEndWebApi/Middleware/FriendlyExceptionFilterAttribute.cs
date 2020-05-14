// <copyright file="FriendlyExceptionFilterAttribute.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
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
                    switch (codedException)
                    {
                        case ConflictException _:
                            context.Result = new ConflictObjectResult(codedException.MessageCode);
                            break;

                        case EntityNotFoundException _:
                            context.Result = new NotFoundObjectResult(codedException.MessageCode);
                            break;

                        case ForbiddenException _:
                            context.Result = new ObjectResult(codedException.MessageCode) { StatusCode = StatusCodes.Status403Forbidden };
                            break;

                        case ProcessingFailedException _:
                            context.Result = new ObjectResult(codedException.MessageCode) { StatusCode = StatusCodes.Status500InternalServerError };
                            break;

                        case UnavailableException _:
                            context.Result = new ObjectResult(codedException.MessageCode) { StatusCode = StatusCodes.Status503ServiceUnavailable };
                            break;

                        default:
                            context.Result = new ObjectResult(codedException.MessageCode) { StatusCode = StatusCodes.Status503ServiceUnavailable };
                            break;
                    }

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
                    context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
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
