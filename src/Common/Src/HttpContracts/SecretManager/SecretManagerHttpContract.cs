// <copyright file="SecretManagerHttpContract.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.SecretManager
{
    /// <summary>
    /// HTTP contract for the secret manager.
    /// </summary>
    public static class SecretManagerHttpContract
    {
        /// <summary>
        /// The v1 API.
        /// </summary>
        public const string ApiV1Route = "api/v1";

        /// <summary>
        /// The resource level secret management route.
        /// </summary>
        public const string ResourceSecretManagerV1Route = ApiV1Route + "/resourcesecrets";

        /// <summary>
        /// The secret management operation.
        /// </summary>
        public const string SecretManagementOperation = "secrets";

        /// <summary>
        /// The secret filter management operation.
        /// </summary>
        public const string FilterManagementOperation = "filters";

        /// <summary>
        /// The get secrets http method.
        /// </summary>
        public static readonly HttpMethod GetSecretsMethod = HttpMethod.Get;

        /// <summary>
        /// The create secret http method.
        /// </summary>
        public static readonly HttpMethod CreateSecretMethod = HttpMethod.Post;

        /// <summary>
        /// The update secret http method.
        /// </summary>
        public static readonly HttpMethod UpdateSecretMethod = HttpMethod.Put;

        /// <summary>
        /// The delete secret http method.
        /// </summary>
        public static readonly HttpMethod DeleteSecretMethod = HttpMethod.Delete;

        /// <summary>
        /// The delete secret filter http method.
        /// </summary>
        public static readonly HttpMethod DeleteSecretFilterMethod = HttpMethod.Delete;

        /// <summary>
        /// Get the get secrets uri, for getting all secrets for given resource ids.
        /// </summary>
        /// <param name="resourceIds">Resource ids.</param>
        /// <returns>Uri.</returns>
        public static string GetGetSecretsUri(IEnumerable<Guid> resourceIds)
        {
            var resourceIdsParam = new StringBuilder();
            foreach (var resourceId in resourceIds)
            {
                resourceIdsParam.Append($"&resourceId={resourceId}");
            }

            return $"{ResourceSecretManagerV1Route}?{resourceIdsParam}";
        }

        /// <summary>
        /// Get the create secret uri.
        /// </summary>
        /// <param name="resourceId">The resource id.</param>
        /// <returns>Uri.</returns>
        public static string GetCreateSecretUri(Guid resourceId) =>
            $"{ResourceSecretManagerV1Route}/{resourceId}/{SecretManagementOperation}";

        /// <summary>
        /// Get the update secret uri.
        /// </summary>
        /// <param name="resourceId">The resource id.</param>
        /// <param name="secretId">The secret id.</param>
        /// <returns>Uri.</returns>
        public static string GetUpdateSecretUri(Guid resourceId, Guid secretId) =>
            $"{ResourceSecretManagerV1Route}/{resourceId}/{SecretManagementOperation}/{secretId}";

        /// <summary>
        /// Get the delete secret uri.
        /// </summary>
        /// <param name="resourceId">The resource id.</param>
        /// <param name="secretId">The secret id.</param>
        /// <returns>Uri.</returns>
        public static string GetDeleteSecretUri(Guid resourceId, Guid secretId) =>
            $"{ResourceSecretManagerV1Route}/{resourceId}/{SecretManagementOperation}/{secretId}";

        /// <summary>
        /// Get the delete secret filter uri.
        /// </summary>
        /// <param name="resourceId">The resource id.</param>
        /// <param name="secretId">The secret id.</param>
        /// <param name="secretFilterType">The secret filter type.</param>
        /// <returns>Uri.</returns>
        public static string GetDeleteSecretFilterUri(Guid resourceId, Guid secretId, SecretFilterType secretFilterType) =>
            $"{ResourceSecretManagerV1Route}/{resourceId}/{SecretManagementOperation}/{secretId}/{FilterManagementOperation}/{secretFilterType}";
    }
}
