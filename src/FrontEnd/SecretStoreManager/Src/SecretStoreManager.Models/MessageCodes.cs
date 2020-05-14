// <copyright file="MessageCodes.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SecretStoreManager.Models
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
        NotReady = 1,

        /// <summary>
        /// Failed to create the secret store.
        /// </summary>
        FailedToCreateSecretStore = 2,

        /// <summary>
        /// Failed to create the secret.
        /// </summary>
        FailedToCreateSecret = 3,

        /// <summary>
        /// User is not authorized to operate on the given secret scope.
        /// </summary>
        UnauthorizedScope = 4,
    }
}
