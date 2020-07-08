// <copyright file="RPaaSMetaRPOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Subscriptions
{
    /// <summary>
    /// Options for the RPaaS MetaRP.
    /// </summary>
    public class RPaaSMetaRPOptions
        : IHttpClientProviderOptions
    {
        /// <inheritdoc/>
        public string BaseAddress { get; set; }
    }
}