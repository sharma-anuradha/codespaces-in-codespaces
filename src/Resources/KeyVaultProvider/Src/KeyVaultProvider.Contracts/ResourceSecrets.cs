// <copyright file="ResourceSecrets.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Contracts
{
    /// <summary>
    /// All secrets stored in a Key Vault resource.
    /// </summary>
    public class ResourceSecrets
    {
        /// <summary>
        /// Gets or sets key vault resource id.
        /// </summary>
        public Guid ResourceId { get; set; }

        /// <summary>
        /// Gets or sets user secrets.
        /// </summary>
        public IEnumerable<UserSecret> UserSecrets { get; set; }
    }
}
