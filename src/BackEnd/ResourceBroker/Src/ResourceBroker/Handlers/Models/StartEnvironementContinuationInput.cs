// <copyright file="StartEnvironementContinuationInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
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
        /// Gets or sets the compute resource id.
        /// </summary>
        public string ComputeResourceId { get; set; }

        /// <summary>
        /// Gets or sets the storage resource id.
        /// </summary>
        public string StorageResourceId { get; set; }

        /// <summary>
        /// Gets or sets the compute environment variables.
        /// </summary>
        public IDictionary<string, string> EnvironmentVariables { get; set; }
    }
}
