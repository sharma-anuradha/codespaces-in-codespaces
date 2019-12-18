// <copyright file="ApplicationServicesProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore
{
    /// <summary>
    /// Convenient place to grab services from the service provider.
    /// </summary>
    public class ApplicationServicesProvider
    {
        /// <summary>
        /// Gets Service Provider.
        /// </summary>
        public static IServiceProvider ServiceProvider { get; internal set; }
    }
}
