// <copyright file="StartInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models
{
    /// <summary>
    /// Environment start input.
    /// </summary>
    public class StartInput
    {
        /// <summary>
        /// Gets or sets the compute resource token id.
        /// </summary>
        public Guid ComputeResourceId { get; set; }

        /// <summary>
        /// Gets or sets the storage resource token id.
        /// </summary>
        public Guid StorageResourceId { get; set; }

        /// <summary>
        /// Gets or sets the compute environment variables.
        /// </summary>
        public IDictionary<string, string> EnvironmentVariables { get; set; }

        /// <summary>
        /// Gets or sets the operation trigger.
        /// </summary>
        public string Trigger { get; set; }
    }
}
