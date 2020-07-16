// <copyright file="VsoPlanKeyVaultProperties.cs" company="Microsoft">
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
    /// KeyVault properties associated with Customer Managed Keys.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class VsoPlanKeyVaultProperties
    {
        /// <summary>
        /// Gets or sets the key name.
        /// </summary>
        public string KeyName { get; set; }

        /// <summary>
        /// Gets or sets the key version.
        /// </summary>
        public string KeyVersion { get; set; }

        /// <summary>
        /// Gets or sets the KeyVault URI.
        /// </summary>
        public string KeyVaultUri { get; set; }

        public static bool operator ==(VsoPlanKeyVaultProperties a, VsoPlanKeyVaultProperties b) =>
a is null ? b is null : a.Equals(b);

        public static bool operator !=(VsoPlanKeyVaultProperties a, VsoPlanKeyVaultProperties b) => !(a == b);

        /// <summary> Tests if this VsoPlanEncryptionProperties equals another VsoPlanEncryptionProperties.</summary>
        /// <param name="other">Another VsoPlanEncryptionProperties.</param>
        /// <returns>True if all VsoPlanEncryptionProperties properties are equal.</returns>
        public bool Equals(VsoPlanKeyVaultProperties other)
        {
            return other != null &&
                other.KeyName == KeyName &&
                other.KeyVersion == KeyVersion &&
                other.KeyVaultUri == KeyVaultUri;
        }

        /// <summary> Tests if this plan settings equals another plan settings.</summary>
        /// <param name="obj">Another plan settings object.</param>
        /// <returns>True if all plan properties are equal.</returns>
        public override bool Equals(object obj) => Equals(obj as VsoPlanKeyVaultProperties);

        /// <summary>Gets a hashcode for the plan settings.</summary>
        /// <returns>Hash code derived from each of the plan settings properties.</returns>
        public override int GetHashCode() => KeyName.GetHashCode() ^
                                                (KeyVersion?.GetHashCode() ?? 0) ^
                                                (KeyVaultUri?.GetHashCode() ?? 0);
    }
}
