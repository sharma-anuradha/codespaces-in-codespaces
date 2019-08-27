// <copyright file="BaseResourceCreateResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class ResourceCreateContinuationResult : ContinuationResult
    {
        /// <summary>
        /// Gets or sets the resource id.
        /// </summary>
        public string ResourceId { get; set; }
    }
}
