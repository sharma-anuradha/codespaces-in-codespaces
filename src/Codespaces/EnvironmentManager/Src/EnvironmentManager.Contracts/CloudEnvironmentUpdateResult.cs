// <copyright file="CloudEnvironmentUpdateResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Class which encapsulates the results of cloud environment update request.
    /// </summary>
    public class CloudEnvironmentUpdateResult
    {
        /// <summary>
        /// Gets the updated environment settings after processing the update request or null if the request was invalid.
        /// </summary>
        public CloudEnvironment CloudEnvironment { get; private set; }

        /// <summary>
        /// Gets the list of errors found while validating the update request or null if the request was valid.
        /// </summary>
        public IEnumerable<MessageCodes> ValidationErrors { get; private set; }

        /// <summary>
        /// Gets additional error details relating to the validation errors or null if no details were provided.
        /// </summary>
        public string ErrorDetails { get; private set; }

        /// <summary>
        /// Gets a value indicating whether or not the result is a successful result.
        /// </summary>
        public bool IsSuccess => ValidationErrors == null;

        /// <summary>
        /// Constructs a CloudEnvironmentUpdateResult for a successul settings update.
        /// </summary>
        /// <param name="updatedEnvironment">The updated environment settings.</param>
        /// <returns>The successful <see cref="CloudEnvironmentUpdateResult"/>.</returns>
        public static CloudEnvironmentUpdateResult Success(CloudEnvironment updatedEnvironment)
        {
            return new CloudEnvironmentUpdateResult
            {
                CloudEnvironment = updatedEnvironment,
                ValidationErrors = null,
                ErrorDetails = null,
            };
        }

        /// <summary>
        /// Constructs a CloudEnvironmentUpdateResult for a failed settings update.
        /// </summary>
        /// <param name="validationErrors">The reason(s) the request failed.</param>
        /// <param name="details">optional parameter: Additional details for message codes.</param>
        /// <returns>The failed <see cref="CloudEnvironmentUpdateResult"/>.</returns>
        public static CloudEnvironmentUpdateResult Error(List<MessageCodes> validationErrors, string details = null)
        {
            return new CloudEnvironmentUpdateResult
            {
                CloudEnvironment = null,
                ValidationErrors = validationErrors,
                ErrorDetails = details,
            };
        }
    }
}
