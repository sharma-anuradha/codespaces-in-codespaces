using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.ComponentModel.DataAnnotations;
using VsClk.EnvReg.Models.Errors;

namespace Microsoft.VsCloudKernel.Services.EnvReg.WebApi.Middleware
{
    // TODO: This should be consolidated with the SDKs UnhandledExceptionReporter
    //       but this will do for the time being.
    public class FriendlyExceptionFilterAttribute : ExceptionFilterAttribute
    {
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
                context.Result = new StatusCodeResult(401);
            }
        }
    }
}
