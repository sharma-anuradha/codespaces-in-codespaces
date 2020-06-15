// <copyright file="ThrottlePerUserAttribute.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.AspNetCore.Http;
using Microsoft.VsSaaS.AspNetCore.Mvc.Filters.Throttling;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Middleware
{
    /// <summary>
    /// Basic user based throttling with configurable overrides.
    /// </summary>
    public class ThrottlePerUserAttribute : ThrottleAttributeBase
    {
        /// <summary>
        /// The default interval requests will be counted over if not specified in the constructor.
        /// </summary>
        protected static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(1);

        private readonly int defaultLimit;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThrottlePerUserAttribute"/> class.
        /// </summary>
        /// <param name="controller">The controller name that this applies to.</param>
        /// <param name="method">The method name that this applies to.</param>
        /// <param name="defaultLimit">The default maximum number of requests if the user doesn't have a configured override.</param>
        /// <param name="interval">The optional interval requests should be counted over.</param>
        public ThrottlePerUserAttribute(string controller, string method, int defaultLimit, TimeSpan? interval = null)
            : base(
                actionName: $"{controller}.{method}",
                interval: interval ?? DefaultInterval)
        {
            this.defaultLimit = defaultLimit;
        }

        /// <inheritdoc />
        protected async override Task<bool> ShouldThrottleAsync(ActionExecutingContext context, int requestCount)
        {
            var userId = GetUserId(context);
            if (string.IsNullOrEmpty(userId))
            {
                return false;
            }

            var configuration = context.HttpContext.RequestServices.GetRequiredService<ISystemConfiguration>();
            var logger = context.HttpContext.GetLogger();

            var limit = await configuration.GetUserIdValueAsync("throttling:max-requests-per-user", userId, logger, defaultLimit);

            return limit < requestCount;
        }

        /// <inheritdoc />
        protected override Task<string> GetThrottleCacheKeyAsync(ActionExecutingContext context)
        {
            return Task.FromResult(GetUserId(context));
        }

        /// <summary>
        /// Gets the current user id from the request context.
        /// </summary>
        /// <remarks>
        /// This is overridable so that child implementations can customize how the user id is stored.
        /// </remarks>
        /// <param name="context">The request context.</param>
        /// <returns>The user id or null if it was not available.</returns>
        protected virtual string GetUserId(ActionExecutingContext context)
        {
            Requires.NotNull(context.HttpContext, nameof(context.HttpContext));

            return context.HttpContext.GetCurrentUserId();
        }
    }
}
