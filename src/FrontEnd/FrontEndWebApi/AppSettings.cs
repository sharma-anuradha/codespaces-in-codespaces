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
    }
}
