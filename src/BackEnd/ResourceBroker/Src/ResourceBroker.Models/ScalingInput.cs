// <copyright file="ScalingInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models
{
    /// <summary>
    /// Scaling request input.
    /// </summary>
    public class ScalingInput
    {
        /// <summary>
        /// Gets or sets the pools that are bing targetted.
        /// </summary>
        public IList<ResourcePool> Pools { get; set; }
    }
}
