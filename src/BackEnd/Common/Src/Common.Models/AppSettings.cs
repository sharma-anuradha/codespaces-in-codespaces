// <copyright file="AppSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models
{
    /// <summary>
    /// Settings that are passed in to the service as config at runtime.
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Gets or sets the git commit used to produce this build. Used for troubleshooting.
        /// </summary>
        public string GitCommit { get; set; }

        /// <summary>
        /// Gets or sets the audiences to accept for JWT tokens.
        /// This should be a comma-delimited list of one or more audiences.
        /// </summary>
        public string AuthJwtAudiences { get; set; }

        /// <summary>
        /// Gets or sets the CosmosDB host.
        /// </summary>
        public string AzureCosmosDbHost { get; set; }

        /// <summary>
        /// Gets or sets the CosmosDB auth key.
        /// </summary>
        public string AzureCosmosDbKey { get; set; }

        /// <summary>
        /// Gets or sets the CosmosDB database to use in the <see cref="AzureCosmosDbHost"/>.
        /// </summary>
        public string AzureCosmosDbId { get; set; }

        /// <summary>
        /// Gets or sets the CosmosDB preferred location.
        /// </summary>
        public string AzureCosmosPreferredLocation { get; set; }

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

        /// <summary>
        /// Gets or sets a value indicating whether mock implementations for the
        /// Backend API service.
        /// </summary>
        public bool UseMocksForBackendApi { get; set; }
    }
}
