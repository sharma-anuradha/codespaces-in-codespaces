// <copyright file="AppSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable SA1649 // File name should match first type name

namespace Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil.Models
{
    public class AppConfiguration
    {
        public AppSettings AppSettings { get; set; }

        public AppSecrets AppSecrets { get; set; }
    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class AppSettings
    {
        public string DeveloperAlias { get; set; }

        public SkuCatalogSettings SkuCatalogSettings { get; set; }

        public bool DeveloperPersonalStamp { get; set; } = true;

        public bool DeveloperKusto { get; set; } = false;

        public bool RedirectStandardOutToLogsDirectory { get; set; } = false;

        public bool GenerateLocalHostNameFromNgrok { get; set; } = false;

        public FrontEnd FrontEnd { get; set; }

        public ControlPlaneSettings ControlPlaneSettings { get; set; }
    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class AppSecrets
    {
        public string AppServicePrincipalClientSecret { get; set; }
    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class SkuCatalogSettings
    {
        public Dictionary<string, ImageFamilySettings> VmAgentImageFamilies { get; set; } = new Dictionary<string, ImageFamilySettings>();
    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class FrontEnd
    {
        public string BackEndWebApiBaseAddress { get; set; }

        public bool UseMocksForLocalDevelopment { get; set; }

        public bool UseBackEndForLocalDevelopment { get; set; }

        public string ForwardingHostForLocalDevelopment { get; set; }
    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class ControlPlaneSettings
    {
        public string EnvironmentName { get; set; }

        public string InstanceName { get; set; }

        public string DnsHostName { get; set; }

        public Dictionary<AzureLocation, ControlPlaneStampSettings> Stamps { get; set; } = new Dictionary<AzureLocation, ControlPlaneStampSettings>();
    }
}
#pragma warning restore SA1649 // File name should match first type name
#pragma warning restore SA1600 // Elements should be documented
#pragma warning restore SA1402 // File may only contain a single type