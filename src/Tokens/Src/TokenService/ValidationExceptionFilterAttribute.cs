// <copyright file="ValidationExceptionFilterAttribute.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.VsSaaS.Services.TokenService
{
    /// <summary>
    /// Exception filter that converts a validation exception to a standard ProblemDetails response.
    /// </summary>
    public class ValidationExceptionFilterAttribute : ExceptionFilterAttribute
    {
        /// <inheritdoc/>
        public override void OnException(ExceptionContext context)
        {
            if (context.Exception is ValidationException validationException)
            {
                if (string.IsNullOrEmpty(validationException.Message))
                {
                    context.Result = new BadRequestResult();
                }
                else
                {
                    context.Result = new BadRequestObjectResult(new ProblemDetails
                    {
                        Title = "Invalid arguments.",
                        Detail = validationException.Message,
                    });
                }
            }
            else if (context.Exception is SecurityTokenValidationException tokenException)
            {
                if (string.IsNullOrEmpty(tokenException.Message))
                {
                    context.Result = new BadRequestResult();
                }
                else
                {
                    context.Result = new BadRequestObjectResult(new ProblemDetails
                    {
                        Title = "Token validation error.",
                        Detail = tokenException.Message,
                    });
                }
            }
        }
    }
}
