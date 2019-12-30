// <copyright file="ThrottlePerUserLowAttribute.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.AspNetCore.Mvc.Filters.Throttling;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Middleware
{
    /// <summary>
    /// Set throttling to low: 5 requests per minute per user.
    /// </summary>
    public class ThrottlePerUserLowAttribute : ThrottleAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ThrottlePerUserLowAttribute"/> class.
        /// </summary>
        /// <param name="controller">The controller name that this applies to.</param>
        /// <param name="method">The method name that this applies to.</param>
        public ThrottlePerUserLowAttribute(string controller, string method)
            : base(
                actionName: $"{controller}.{method}",
                strategy: ThrottleKeyStrategy.UserId,
                count: 5,
                interval: TimeSpan.FromMinutes(1))
        {
        }
    }
}
