// <copyright file="ResourceBrokerLoggingExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker
{
    /// <summary>
    /// Logging helper for <see cref="ResourceBrokerResource"/>, <see cref="CreateResourceRequestBody"/>, and <see cref="StartResourceRequestBody"/>.
    /// </summary>
    public static class ResourceBrokerLoggingExtensions
    {
        /// <summary>
        /// Add a resource id token to the logging context.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="resourceId">The resource id otken value.</param>
        /// <returns>The <paramref name="logger"/> instance.</returns>
        public static IDiagnosticsLogger AddBaseResourceId(this IDiagnosticsLogger logger, Guid? resourceId)
        {
            return logger.FluentAddBaseValue(nameof(ResourceBrokerResource.ResourceId), resourceId?.ToString());
        }

        /// <summary>
        /// Add a <see cref="StartResourceRequestBody"/> to the logging context.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="value">The start compute request body instance.</param>
        /// <returns>The <paramref name="logger"/> instance.</returns>
        public static IDiagnosticsLogger AddStartResourceRequest(this IDiagnosticsLogger logger, StartResourceRequestBody value)
        {
            var environmentVariables = value != null ? JsonConvert.SerializeObject(value?.EnvironmentVariables, Formatting.Indented) : null;
            return logger
                .FluentAddValue(nameof(StartResourceRequestBody.StorageResourceId), value?.StorageResourceId.ToString())
                .FluentAddValue(nameof(StartResourceRequestBody.EnvironmentVariables), environmentVariables);
        }
    }
}
