// <copyright file="ErrorCodes.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// List error codes returned by <see cref="ICloudEnvironmentManager"/>.
    /// </summary>
    internal static class ErrorCodes
    {
        /// <summary>
        /// Quota exceeded.
        /// </summary>
        public const string ExceededQuota = "ErrorExceededQuota";

        /// <summary>
        /// Environment name specified already exists.
        /// </summary>
        public const string EnvironmentNameAlreadyExists = "EnvironmentNameAlreadyExists";

        /// <summary>
        /// Unknown.
        /// </summary>
        public const string Unknown = "Unknown";
    }
}
