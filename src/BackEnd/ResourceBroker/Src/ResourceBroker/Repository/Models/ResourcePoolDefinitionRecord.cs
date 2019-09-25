// <copyright file="ResourcePoolDefinitionRecord.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models
{
    /// <summary>
    /// Resource pool record.
    /// </summary>
    public class ResourcePoolDefinitionRecord
    {
        /// <summary>
        /// Gets or sets the definition code.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the version definition code.
        /// </summary>
        public string VersionCode { get; set; }

        /// <summary>
        /// Gets or sets the resource pool dimensions
        /// </summary>
        public IDictionary<string, string> Dimensions { get; set; }
    }
}
