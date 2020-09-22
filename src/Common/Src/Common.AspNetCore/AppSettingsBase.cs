// <copyright file="AppSettingsBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
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
        /// Gets or sets the First Party App settings.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public FirstPartyAppSettings FirstPartyAppSettings { get; set; }

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
        /// Gets or sets the per family quota settings.
        /// </summary>
        public IDictionary<string, IDictionary<string, int>> QuotaFamilySettings { get; set; }

        /// <summary>
        /// Gets or sets the Plan SKU catalog settings.
        /// </summary>
        public PlanSkuCatalogSettings PlanSkuCatalogSettings { get; set; }

        /// <summary>
        /// Gets or sets the data-plane settings.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public DataPlaneSettings DataPlaneSettings { get; set; }

        /// <summary>
        /// Gets or sets the authentication settings.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public AuthenticationSettings AuthenticationSettings { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether developer personal azure resources should be used.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public bool DeveloperPersonalStamp { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether developer kusto table should be used.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public bool DeveloperKusto { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to set the local hostname from Ngrok for development.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public bool GenerateLocalHostNameFromNgrok { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to redirect output from standard out to the logs directory.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public bool RedirectStandardOutToLogsDirectory { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to start the Diagnostics Server.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public bool StartDiagnosticsServer { get; set; } = false;

        /// <summary>
        /// Gets or sets the unique identifier for developer for developer stamps.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string DeveloperAlias { get; set; }

        /// <summary>
        /// Gets or sets the MDSD event source for the metrics logger.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string MetricsLoggerMdsdEventSource { get; set; }

        /// <summary>
        /// Gets or sets the Agent settings.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public AgentSettings AgentSettings { get; set; }

        /// <summary>
        /// Gets or sets the container name used for the claim distributed.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string ClaimDistributedContainerName { get; set; }

        /// <summary>
        /// Gets or sets the build Id of this build. Used for troubleshooting.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string BuildId { get; set; }

        /// <summary>
        /// Gets or sets the build number of this build. Used for troubleshooting.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string BuildNumber { get; set; }
    }
}
