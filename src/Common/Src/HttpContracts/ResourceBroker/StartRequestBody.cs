// <copyright file="StartRequestBody.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker
{
    /// <summary>
    /// Start Request Body.
    /// </summary>
    public class StartRequestBody
    {
        /// <summary>
        /// Gets or sets the storage resource id token.
        /// </summary>
        [Required]
        public Guid ResourceId { get; set; }

        /// <summary>
        /// Gets or sets the environment variable dictionary for the environment compute.
        /// </summary>
        public Dictionary<string, string> Variables { get; set; }
    }
}
