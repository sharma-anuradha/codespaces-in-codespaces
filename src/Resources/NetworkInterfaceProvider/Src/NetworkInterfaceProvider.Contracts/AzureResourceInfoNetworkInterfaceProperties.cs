// <copyright file="AzureResourceInfoNetworkInterfaceProperties.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.NetworkInterfaceProvider.Contracts
{
    /// <summary>
    /// Azure resource info properties for the network interface.
    /// </summary>
    public class AzureResourceInfoNetworkInterfaceProperties : Dictionary<string, string>
    {
        /// <summary>
        /// Key name of the vnet.
        /// </summary>
        private const string VNetName = "vnet";

        /// <summary>
        /// Key name of the network security group.
        /// </summary>
        private const string NsgName = "nsg";

        /// <summary>
        /// Key name of the is vnet injected flag.
        /// </summary>
        private const string IsVNetInjectedName = "vNetInjection";

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureResourceInfoNetworkInterfaceProperties"/> class.
        /// </summary>
        public AzureResourceInfoNetworkInterfaceProperties()
            : this(new Dictionary<string, string>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureResourceInfoNetworkInterfaceProperties"/> class.
        /// </summary>
        /// <param name="properties">Azure resource info properties.</param>
        public AzureResourceInfoNetworkInterfaceProperties(IDictionary<string, string> properties)
            : base(properties ?? new Dictionary<string, string>())
        {
        }

        /// <summary>
        /// Gets or sets the vnet name or null if not available.
        /// </summary>
        public string VNet
        {
            get { return TryGetPropertyValue(VNetName); }

            set { SetPropertyValue(VNetName, value); }
        }

        /// <summary>
        /// Gets or sets the network security group name or null if not available.
        /// </summary>
        public string Nsg
        {
            get { return TryGetPropertyValue(NsgName); }

            set { SetPropertyValue(NsgName, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the vnet was injected from a customer.
        /// </summary>
        public bool IsVNetInjected
        {
            // For backwards compatibility, a missing value is considered as a "1"
            get { return TryGetPropertyValue(IsVNetInjectedName) != "0"; }

            set { SetPropertyValue(IsVNetInjectedName, value ? "1" : "0"); }
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
