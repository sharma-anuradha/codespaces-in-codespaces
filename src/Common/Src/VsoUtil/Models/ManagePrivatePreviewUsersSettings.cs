// <copyright file="ManagePrivatePreviewUsersSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable SA1649 // File name should match first type name

namespace Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil.Models.PrivatePreview
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class ManagePrivatePreviewUsersSettings
    {
        public IList<DatabaseInfo> Databases { get; set; }

        public IList<string> AadScopes { get; set; }

        public string AzureClientId { get; set; }

        public string AzureAuthority { get; set; }
    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class DatabaseInfo
    {
        public string LiveShareEnvironment { get; set; }

        public string Uri { get; set; }

        public DatabaseCredentials Credentials { get; set; }

        public string DatabaseId { get; set; }

        public string ContainerId { get; set; }

        public AzureInfo AzureInfo { get; set; }
    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class AzureInfo
    {
        public string SubscriptionId { get; set; }

        public string ResourceGroup { get; set; }

        public string DatabaseAccount { get; set; }
    }

    public class DatabaseCredentials
    {
        public string PrimaryMasterKey { get; set; }

        public string SecondaryMasterKey { get; set; }

        public string PrimaryReadonlyMasterKey { get; set; }

        public string SecondaryReadonlyMasterKey { get; set; }
    }
}
#pragma warning restore SA1649 // File name should match first type name
#pragma warning restore SA1600 // Elements should be documented
#pragma warning restore SA1402 // File may only contain a single type