// <copyright file="ControlPlaneSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// The standard azure resource settings.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class ControlPlaneSettings
    {
        /// <summary>
        /// Gets or sets the subscription id.
        /// This value is optional because it can be queried at runtime when running in azure.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets the github sp id.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string GithubSpId { get; set; }

        /// <summary>
        /// Gets or sets the name prefix.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Prefix { get; set; }

        /// <summary>
        /// Gets or sets the service base name.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string ServiceName { get; set; }

        /// <summary>
        /// Gets or sets short prefix.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string ShortPrefix { get; set; }

        /// <summary>
        /// Gets or sets the environment name.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string EnvironmentName { get; set; }

        /// <summary>
        /// Gets or sets the instance name.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string InstanceName { get; set; }

        /// <summary>
        /// Gets or sets the DNS host name.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string DnsHostName { get; set; }

        /// <summary>
        /// Gets or sets the name of the blob container that the Resource Broker can use for file share template blobs.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string FileShareTemplateContainerName { get; set; }

        /// <summary>
        /// Gets or sets the name of the blob container that the Resource Broker can use for vso agent blobs.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string VirtualMachineAgentContainerName { get; set; }

        /// <summary>
        /// Gets or sets the control-plane stamps.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public Dictionary<AzureLocation, ControlPlaneStampSettings> Stamps { get; set; } = new Dictionary<AzureLocation, ControlPlaneStampSettings>();
    }
}
