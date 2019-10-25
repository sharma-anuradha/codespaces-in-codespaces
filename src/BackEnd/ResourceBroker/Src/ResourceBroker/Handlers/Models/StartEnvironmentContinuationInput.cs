// <copyright file="StartEnvironmentContinuationInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models
{
    /// <summary>
    /// Start compute continuation input.
    /// </summary>
    public class StartEnvironmentContinuationInput : ContinuationOperationInput
    {
        /// <summary>
        /// Gets or sets the storage resource id.
        /// </summary>
        public Guid StorageResourceId { get; set; }

        /// <summary>
        /// Gets or sets the compute environment variables.
        /// </summary>
        public IDictionary<string, string> EnvironmentVariables { get; set; }
    }
}
