// <copyright file="ArmThrottlePerUserAttribute.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Middleware
{
    /// <summary>
    /// Throttling for ARM requests, specifically for use in <see cref="SubscriptionsController"/>.
    /// The user's id is generated from claims which are set by <see cref="AuthenticationBuilderRPSaasExtensions"/>.
    /// </summary>
    public class ArmThrottlePerUserAttribute : ThrottlePerUserAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ArmThrottlePerUserAttribute"/> class.
        /// </summary>
        /// <param name="controller">The controller name that this applies to.</param>
        /// <param name="method">The method name that this applies to.</param>
        public ArmThrottlePerUserAttribute(string controller, string method)
            : base(controller, method, 100)
        {
        }

        /// <inheritdoc />
        protected override string GetUserId(ActionExecutingContext context)
        {
            return context.HttpContext.User.GetUserIdFromClaims();
        }

        /// <inheritdoc />
        protected override Task OnThrottledAsync(ActionExecutingContext context)
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
            context.Result = ResourceProviderErrorResponse.Create("RequestThrottled", RateLimitExceededMessage);

            return Task.CompletedTask;
        }
    }
}
