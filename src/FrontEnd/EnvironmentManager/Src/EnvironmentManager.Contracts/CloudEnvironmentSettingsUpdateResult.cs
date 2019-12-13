// <copyright file="CloudEnvironmentSettingsUpdateResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Class which encapsulates the results of cloud environment settings update request.
    /// </summary>
    public class CloudEnvironmentSettingsUpdateResult
    {
        /// <summary>
        /// Gets the updated environment settings after processing the update request or null if the request was invalid.
        /// </summary>
        public CloudEnvironment CloudEnvironment { get; private set; }

        /// <summary>
        /// Gets the list of errors found while validating the update request or null if the request was valid.
        /// </summary>
        public IEnumerable<ErrorCodes> ValidationErrors { get; private set; }

        /// <summary>
        /// Gets a value indicating whether or not the result is a successful result.
        /// </summary>
        public bool IsSuccess => ValidationErrors == null;

        /// <summary>
        /// Constructs a CloudEnvironmentSettingsUpdateResult for a successul settings update.
        /// </summary>
        /// <param name="updatedEnvironment">The updated environment settings.</param>
        /// <returns>The successful <see cref="CloudEnvironmentSettingsUpdateResult"/>.</returns>
        public static CloudEnvironmentSettingsUpdateResult Success(CloudEnvironment updatedEnvironment)
        {
            return new CloudEnvironmentSettingsUpdateResult
            {
                CloudEnvironment = updatedEnvironment,
                ValidationErrors = null,
            };
        }

        /// <summary>
        /// Constructs a CloudEnvironmentSettingsUpdateResult for a failed settings update.
        /// </summary>
        /// <param name="validationErrors">The reason(s) the request failed.</param>
        /// <returns>The failed <see cref="CloudEnvironmentSettingsUpdateResult"/>.</returns>
        public static CloudEnvironmentSettingsUpdateResult Error(List<ErrorCodes> validationErrors)
        {
            return new CloudEnvironmentSettingsUpdateResult
            {
                CloudEnvironment = null,
                ValidationErrors = validationErrors,
            };
        }
    }
}
