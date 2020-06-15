// <copyright file="SecretFilterData.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Models
{
    /// <summary>
    /// Secret filter data against which filters on the secrets will be matched.
    /// This object conatins a filter type and the corresponding value from the cloudEnvironment record.
    /// </summary>
    public class SecretFilterData
    {
        /// <summary>
        /// Gets or sets secret filter type.
        /// </summary>
        public SecretFilterType Type { get; set; }

        /// <summary>
        /// Gets or sets data against which the filters of the <see cref="Type"/> will be compared against.
        /// </summary>
        public string Data { get; set; }
    }
}