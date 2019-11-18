// <copyright file="ResourceBrokerLoggingExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker
{
    /// <summary>
    /// Logging helper for <see cref="ResourceBrokerResource"/>, <see cref="CreateResourceRequestBody"/>, and <see cref="StartComputeRequestBody"/>.
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
        /// Add a <see cref="StartComputeRequestBody"/> to the logging context.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="value">The start compute request body instance.</param>
        /// <returns>The <paramref name="logger"/> instance.</returns>
        public static IDiagnosticsLogger AddStartComputeRequest(this IDiagnosticsLogger logger, StartComputeRequestBody value)
        {
            var environmentVariables = value != null ? JsonConvert.SerializeObject(value?.EnvironmentVariables, Formatting.Indented) : null;
            return logger
                .FluentAddValue(nameof(StartComputeRequestBody.StorageResourceId), value?.StorageResourceId.ToString())
                .FluentAddValue(nameof(StartComputeRequestBody.EnvironmentVariables), environmentVariables);
        }
    }
}
