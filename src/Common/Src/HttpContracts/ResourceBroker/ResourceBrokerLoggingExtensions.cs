// <copyright file="ResourceBrokerLoggingExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

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
        /// <param name="resourceIdToken">The resource id otken value.</param>
        /// <returns>The <paramref name="logger"/> instance.</returns>
        public static IDiagnosticsLogger AddResourceIdToken(this IDiagnosticsLogger logger, string resourceIdToken)
        {
            return logger.FluentAddValue(nameof(ResourceBrokerResource.ResourceIdToken), resourceIdToken);
        }

        /// <summary>
        /// Add a <see cref="ResourceBrokerResource"/> to the logging context.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="value">The resource broker resource instance.</param>
        /// <returns>The <paramref name="logger"/> instance.</returns>
        public static IDiagnosticsLogger AddResourceBrokerResource(this IDiagnosticsLogger logger, ResourceBrokerResource value)
        {
            return logger
                .AddResourceIdToken(value?.ResourceIdToken)
                .FluentAddValue(nameof(ResourceBrokerResource.Created), value?.Created.ToUniversalTime().ToString("u"))
                .FluentAddValue(nameof(ResourceBrokerResource.Location), value?.Location.ToString())
                .FluentAddValue(nameof(ResourceBrokerResource.SkuName), value?.SkuName)
                .FluentAddValue(nameof(ResourceBrokerResource.Type), value?.Type.ToString());
        }

        /// <summary>
        /// Add a <see cref="CreateResourceRequestBody"/> to the logging context.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="value">The create resource request.</param>
        /// <returns>The <paramref name="logger"/> instance.</returns>
        public static IDiagnosticsLogger AddCreateResourceRequestBody(this IDiagnosticsLogger logger, CreateResourceRequestBody value)
        {
            return logger
                .FluentAddValue(nameof(CreateResourceRequestBody.Location), value?.Location.ToString())
                .FluentAddValue(nameof(CreateResourceRequestBody.SkuName), value?.SkuName)
                .FluentAddValue(nameof(CreateResourceRequestBody.Type), value?.Type.ToString());
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
                .FluentAddValue(nameof(StartComputeRequestBody.StorageResourceIdToken), value?.StorageResourceIdToken)
                .FluentAddValue(nameof(StartComputeRequestBody.EnvironmentVariables), environmentVariables);
        }
    }
}
