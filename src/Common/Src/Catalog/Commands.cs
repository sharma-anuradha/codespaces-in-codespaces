// <copyright file="Commands.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Catalog
{
    /// <summary>
    /// Implement various catalog commands.
    /// </summary>
    public static class Commands
    {
        /// <summary>
        /// Write the SKU catalog.
        /// </summary>
        /// <param name="skuCatalog">The SKU catalog.</param>
        /// <param name="textWriter">The text writer, or null for Console.</param>
        public static void WriteSkuCatalog(ISkuCatalog skuCatalog, TextWriter textWriter = null)
        {
            textWriter = textWriter ?? Console.Out;

            var orderedSkus = new SortedDictionary<string, ICloudEnvironmentSku>();
            foreach (var item in skuCatalog.CloudEnvironmentSkus)
            {
                orderedSkus.Add(item.Key, item.Value);
            }

            var skusJson = JsonConvert.SerializeObject(orderedSkus, Formatting.Indented);
            textWriter.WriteLine(skusJson);
        }

        /// <summary>
        /// Write the azure subscription catalog.
        /// </summary>
        /// <param name="azureSubscriptionCatalog">The subscription catalog.</param>
        /// <param name="textWriter">The text writer, or null for Console.</param>
        public static void WriteAzureSubscriptionCatalog(IAzureSubscriptionCatalog azureSubscriptionCatalog, TextWriter textWriter = null)
        {
            textWriter = textWriter ?? Console.Out;

            var subscriptions = JsonConvert.SerializeObject(
                azureSubscriptionCatalog.AzureSubscriptions
                    .OrderBy(s => s.DisplayName)
                    .ToDictionary(item => item.DisplayName, item => item),
                Formatting.Indented);
            textWriter.WriteLine(subscriptions);
        }

        /// <summary>
        /// Write the control plane info.
        /// </summary>
        /// <param name="controlPlaneInfo">The control plane info.</param>
        /// <param name="textWriter">The text writer, or null for Console.</param>
        public static void WriteControlPlaneInfo(IControlPlaneInfo controlPlaneInfo, TextWriter textWriter = null)
        {
            textWriter = textWriter ?? Console.Out;
            var info = JsonConvert.SerializeObject(controlPlaneInfo, Formatting.Indented);
            textWriter.WriteLine(info);
        }
    }
}
