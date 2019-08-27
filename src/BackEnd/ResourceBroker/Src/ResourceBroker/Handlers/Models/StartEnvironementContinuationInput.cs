// <copyright file="StartEnvironementContinuationInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class StartEnvironementContinuationInput
    {
        /// <summary>
        /// Gets or sets the compute resource token id.
        /// </summary>
        public ResourceId ComputeResourceId { get; set; }

        /// <summary>
        /// Gets or sets the storage resource token id.
        /// </summary>
        public string StorageResourceId { get; set; }

        /// <summary>
        /// Gets or sets the compute environment variables.
        /// </summary>
        public IDictionary<string, string> EnvironmentVariables { get; set; }
    }
}
