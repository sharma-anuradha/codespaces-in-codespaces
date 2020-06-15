// <copyright file="ThrottlePerUserHighAttribute.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Middleware
{
    /// <summary>
    /// Set throttling to high: 100 requests per minute per user.
    /// </summary>
    public class ThrottlePerUserHighAttribute : ThrottlePerUserAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ThrottlePerUserHighAttribute"/> class.
        /// </summary>
        /// <param name="controller">The controller name that this applies to.</param>
        /// <param name="method">The method name that this applies to.</param>
        public ThrottlePerUserHighAttribute(string controller, string method)
            : base(controller, method, 100)
        {
        }
    }
}
