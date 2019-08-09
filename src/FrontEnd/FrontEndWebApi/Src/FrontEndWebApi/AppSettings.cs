// <copyright file="AppSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi
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
        /// Gets or sets the authority to use for JWT token validation.
        /// </summary>
        public string AuthJwtAuthority { get; set; }

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
        public string AzureCosmosDbAuthKey { get; set; }

        /// <summary>
        /// Gets or sets the CosmosDB database to use in the <see cref="AzureCosmosDbHost"/>.
        /// </summary>
        public string AzureCosmosDbDatabaseId { get; set; }

        /// <summary>
        /// Gets or sets the base address for the back-end web api.
        /// </summary>
        public string BackEndWebApiBaseAddress { get; set; }

        /// <summary>
        /// Gets or sets default host for the session callback.
        /// </summary>
        public string SessionCallbackDefaultHost { get; set; }

        /// <summary>
        /// Gets or sets the session callback default path.
        /// </summary>
        public string SessionCallbackDefaultPath { get; set; }

        /// <summary>
        /// Gets or sets the preferred schema for the cloud environment callback.
        /// </summary>
        public string SessionCallbackPreferredSchema { get; set; }

        /// <summary>
        /// Gets or sets the Live Share API endpoint.
        /// </summary>
        public string VSLiveShareApiEndpoint { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use mock providers for local development.
        /// </summary>
        public bool UseMocksForLocalDevelopment { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to call the real backend during local development instead of mocks.
        /// </summary>
        public bool UseBackEndForLocalDevelopment { get; set; }
    }
}
