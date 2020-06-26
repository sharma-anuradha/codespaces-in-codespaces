// <copyright file="StartInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts
{
    /// <summary>
    /// Environment start input.
    /// </summary>
    public class StartInput
    {
        /// <summary>
        /// Gets or sets the storage resource id token.
        /// </summary>
        public Guid ResourceId { get; set; }

        /// <summary>
        /// Gets or sets the environment variable dictionary for the environment compute.
        /// </summary>
        public Dictionary<string, string> Variables { get; set; }

        /// <summary>
        /// Gets or sets data required for computing applicable secrets for the environment.
        /// </summary>
        public FilterSecretsInput FilterSecrets { get; set; }
    }
}
