// <copyright file="RedisCacheOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication
{
    /// <summary>
    /// Options for accessing the redis cache.
    /// </summary>
    public class RedisCacheOptions
    {
        /// <summary>
        /// Gets or sets the redis cache connection string.
        /// </summary>
        public string RedisConnectionString { get; set; }
    }
}
