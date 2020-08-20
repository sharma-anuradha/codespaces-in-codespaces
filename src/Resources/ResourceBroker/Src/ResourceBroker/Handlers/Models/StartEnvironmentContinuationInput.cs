// <copyright file="StartEnvironmentContinuationInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models
{
    /// <summary>
    /// Start compute continuation input.
    /// </summary>
    public class StartEnvironmentContinuationInput : BaseStartEnvironmentContinuationInput
    {
        /// <summary>
        /// Gets or sets the user secrets.
        /// </summary>
        public IEnumerable<UserSecretData> UserSecrets { get; set; }
    }
}
