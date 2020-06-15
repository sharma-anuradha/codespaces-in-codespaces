// <copyright file="UserSecretData.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Models
{
    /// <summary>
    /// Data contract for sending user secrets to the VM.
    /// </summary>
    public class UserSecretData
    {
        /// <summary>
        /// Gets or sets secret type.
        /// </summary>
        public SecretType Type { get; set; }

        /// <summary>
        /// Gets or sets secret name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets secret value.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Determine the equality by secret name and type.
        /// </summary>
        /// <param name="obj">Another user secret to compare.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return obj is UserSecretData data &&
                   Type == data.Type &&
                   Name == data.Name;
        }

        /// <summary>
        /// Default hash function to calculate hash based on secret name and type.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            var hashCode = -1979447941;
            hashCode = (hashCode * -1521134295) + Type.GetHashCode();
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(Name);
            return hashCode;
        }
    }
}
