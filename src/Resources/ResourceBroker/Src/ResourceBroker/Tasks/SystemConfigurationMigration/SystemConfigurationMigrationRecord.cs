// <copyright file="SystemConfigurationMigrationRecord.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.Repository.Models;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks.SystemConfigurationMigration
{
    /// <summary>
    /// Configuration Migration Record.
    /// </summary>
    public class SystemConfigurationMigrationRecord : SystemConfigurationRecord
    {
        /// <summary>
        /// Gets or sets migration status of a record.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "migrated")]
        public bool Migrated { get; set; }
    }
}
