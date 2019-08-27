// <copyright file="EnvironmentStartResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class EnvironmentStartResult : ContinuationResult
    {
        /// <summary>
        /// Gets or sets example: '/subscriptions/2fa47206-c4b5-40ff-a5e6-9160f9ee000c/storage/<uid>'
        /// </summary>
        public ResourceId ResourceId { get; set; }
    }
}
