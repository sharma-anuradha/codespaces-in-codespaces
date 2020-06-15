// <copyright file="MocksSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models
{
    /// <summary>
    /// Settings for using mocks at runtime.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class MocksSettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether the option which controls whether the
        /// system loads in memory data store for repositories to make inner loop faster.
        /// </summary>
        public bool UseMocksForExternalDependencies { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether mock implementations for the
        /// Resource Broker.
        /// </summary>
        public bool UseMocksForResourceBroker { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether mock implementations for the
        /// Storage and Compute Providers.
        /// </summary>
        public bool UseMocksForResourceProviders { get; set; }
    }
}
