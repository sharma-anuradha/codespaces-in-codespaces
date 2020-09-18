// <copyright file="DiagnosticLoggerExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="IDiagnosticsLogger"/>
    /// </summary>
    public static class DiagnosticLoggerExtensions
    {
        /// <summary>
        /// Adds properties of the <paramref name="azureResource"/> to the <paramref name="logger"/> as base values.
        /// </summary>
        /// <param name="logger">the logger</param>
        /// <param name="azureResource">the azure resource</param>
        /// <returns>The logger with base values added</returns>
        public static IDiagnosticsLogger AddBaseAzureResource(this IDiagnosticsLogger logger, GenericResourceInner azureResource)
        {
            return logger
                .FluentAddBaseValue("AzureResourceType", azureResource.Type)
                .FluentAddBaseValue("AzureResourceId", azureResource.Id)
                .FluentAddBaseValue("ResourceLocation", azureResource.Location)
                .FluentAddBaseValue("AzureResourceTags", JsonConvert.SerializeObject(azureResource.Tags));
        }
    }
}
