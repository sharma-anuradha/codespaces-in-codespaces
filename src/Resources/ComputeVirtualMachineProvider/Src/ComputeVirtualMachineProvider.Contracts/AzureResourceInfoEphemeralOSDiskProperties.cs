// <copyright file="AzureResourceInfoEphemeralOSDiskProperties.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts
{
    /// <summary>
    /// Azure resource info properties for the empheral os disk.
    /// </summary>
    public class AzureResourceInfoEphemeralOSDiskProperties : Dictionary<string, string>
    {
        /// <summary>
        /// Key name of the is uses ephemeral OS disk flag.
        /// </summary>
        private const string UsesEphemeralOSDiskName = "usesEphemeralOSDisk";

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureResourceInfoEphemeralOSDiskProperties"/> class.
        /// </summary>
        public AzureResourceInfoEphemeralOSDiskProperties()
            : this(new Dictionary<string, string>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureResourceInfoEphemeralOSDiskProperties"/> class.
        /// </summary>
        /// <param name="properties">Azure resource info properties.</param>
        public AzureResourceInfoEphemeralOSDiskProperties(IDictionary<string, string> properties)
            : base(properties ?? new Dictionary<string, string>())
        {
        }

        /// <summary>
        /// Gets or sets a value indicating whether ephemeral OS disk was used.
        /// </summary>
        public bool UsesEphemeralOSDisk
        {
            get { return TryGetPropertyValue(UsesEphemeralOSDiskName) == "true"; }

            set { SetPropertyValue(UsesEphemeralOSDiskName, value ? "true" : "false"); }
        }

        private string TryGetPropertyValue(string key)
        {
            if (!TryGetValue(key, out var value))
            {
                return null;
            }

            return value;
        }

        private void SetPropertyValue(string key, string value)
        {
            this[key] = value;
        }
    }
}
