// <copyright file="MessageCodes.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SecretStoreManager.Contracts
{
    /// <summary>
    /// List error codes returned by SecretStoreManager.
    /// </summary>
    public enum MessageCodes
    {
        /// <summary>
        /// Unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Secret store is not ready.
        /// </summary>
        NotReady = 10,

        /// <summary>
        /// Failed to create the secret store.
        /// </summary>
        FailedToCreateSecretStore = 20,

        /// <summary>
        /// Failed to create the secret.
        /// </summary>
        FailedToCreateSecret = 30,

        /// <summary>
        /// User is not authorized to operate on the given secret scope.
        /// </summary>
        UnauthorizedScope = 40,

        /// <summary>
        /// Failed to update the secret.
        /// </summary>
        FailedToUpdateSecret = 50,

        /// <summary>
        /// Failed to delete the secret.
        /// </summary>
        FailedToDeleteSecret = 60,

        /// <summary>
        /// Failed to delete the secret filter.
        /// </summary>
        FailedToDeleteSecretFilter = 70,

        /// <summary>
        /// Secret not found
        /// </summary>
        SecretNotFound = 80,

        /// <summary>
        /// Exceeded secrets quota.
        /// </summary>
        ExceededSecretsQuota = 90,
    }
}
