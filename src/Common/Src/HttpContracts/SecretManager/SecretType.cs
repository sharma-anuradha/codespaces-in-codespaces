// <copyright file="SecretType.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.SecretManager
{
    /// <summary>
    /// User secret type.
    /// </summary>
    public enum SecretType
    {
        /// <summary>
        /// Environment variable.
        /// </summary>
        EnvironmentVariable = 1,
    }
}