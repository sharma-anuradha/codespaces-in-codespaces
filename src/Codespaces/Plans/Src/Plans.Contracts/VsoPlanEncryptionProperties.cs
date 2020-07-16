// <copyright file="VsoPlanEncryptionProperties.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts
{
    /// <summary>
    /// Encryption settings associated with customer associated with Customer Managed Encryption.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class VsoPlanEncryptionProperties
    {
        /// <summary>
        /// Gets or sets the encryption properties.
        /// </summary>
        public VsoPlanKeyVaultProperties KeyVaultProperties { get; set; }

        /// <summary>
        /// Gets or sets the KeySource, either "Microsoft.Codespaces" or "Microsoft.KeyVault".
        /// </summary>
        public string KeySource { get; set; }

        public static bool operator ==(VsoPlanEncryptionProperties a, VsoPlanEncryptionProperties b) =>
   a is null ? b is null : a.Equals(b);

        public static bool operator !=(VsoPlanEncryptionProperties a, VsoPlanEncryptionProperties b) => !(a == b);

        /// <summary> Tests if this VsoPlanEncryptionProperties equals another VsoPlanEncryptionProperties.</summary>
        /// <param name="other">Another VsoPlanEncryptionProperties.</param>
        /// <returns>True if all VsoPlanEncryptionProperties properties are equal.</returns>
        public bool Equals(VsoPlanEncryptionProperties other)
        {
            return other != null &&
                other.KeySource == KeySource &&
                other.KeyVaultProperties == KeyVaultProperties;
        }

        /// <summary> Tests if this plan settings equals another plan settings.</summary>
        /// <param name="obj">Another plan settings object.</param>
        /// <returns>True if all plan properties are equal.</returns>
        public override bool Equals(object obj) => Equals(obj as VsoPlanEncryptionProperties);

        /// <summary>Gets a hashcode for the plan settings.</summary>
        /// <returns>Hash code derived from each of the plan settings properties.</returns>
        public override int GetHashCode() => KeySource.GetHashCode() ^
                                                (KeyVaultProperties?.GetHashCode() ?? 0);
    }
}
