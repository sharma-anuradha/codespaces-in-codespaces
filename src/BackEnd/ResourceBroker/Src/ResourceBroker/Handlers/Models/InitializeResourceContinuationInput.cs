﻿// <copyright file="InitializeResourceContinuationInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models
{
    /// <summary>
    /// Create compute continuation input.
    /// </summary>
    public class InitializeResourceContinuationInput : ContinuationOperationInput
    {
        /// <summary>
        /// Gets or sets the pool type.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public ResourceType Type { get; set; }
    }
}
