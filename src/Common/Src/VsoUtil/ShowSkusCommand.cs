// <copyright file="ShowSkusCommand.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil
{

    /// <summary>
    /// The show skus verb.
    /// </summary>
    [Verb("skus", HelpText = "Show the SKU catalog.")]
    public class ShowSkusCommand : CommandBase
    {
        /// <inheritdoc/>
        protected override void ExecuteCommand(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            var skuCatalog = services.GetRequiredService<ISkuCatalog>();
            var orderedSkus = new SortedDictionary<string, ICloudEnvironmentSku>();
            foreach (var item in skuCatalog.CloudEnvironmentSkus)
            {
                orderedSkus.Add(item.Key, item.Value);
            }

            var skusJson = JsonSerializeObject(orderedSkus);
            stdout.WriteLine(skusJson);
        }
    }
}
