// <copyright file="AppSettingsBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore
{
    /// <summary>
    /// A base class for AppSettings that are common to both the front-enad and the back-end.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class AppSettingsBase
    {
        /// <summary>
        /// Gets or sets the database ID to be used with the Cosmos DB accounts.
        /// Assumes that the same ID is used for both the instance-level and the stamp-level accounts.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string AzureCosmosDbDatabaseId { get; set; }

        /// <summary>
        /// Gets or sets the git commit used to produce this build. Used for troubleshooting.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string GitCommit { get; set; }

        /// <summary>
        /// Gets or sets the application service principal.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public ServicePrincipalSettings ApplicationServicePrincipal { get; set; }

        /// <summary>
        /// Gets or sets the control-plane settings.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public ControlPlaneSettings ControlPlaneSettings { get; set; }

        /// <summary>
        /// Gets or sets the SKU catalog settings.
        /// </summary>
        public SkuCatalogSettings SkuCatalogSettings { get; set; }

        /// <summary>
        /// Gets or sets the data-plane settings.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public DataPlaneSettings DataPlaneSettings { get; set; }

        /// <summary>
        /// Gets or sets the certificate settings.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public CertificateSettings CertificateSettings { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether developer personal azure resources should be used.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public bool DeveloperPersonalStamp { get; set; } = false;
    }
}
