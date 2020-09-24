// <copyright file="GetSkuPropertyCommandBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.IO;
using System.Linq;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil
{
    public abstract class GetSkuPropertyCommandBase : CommandBase
    {
        /// <summary>
        /// Gets or sets the name of the sku whose image version to return.
        /// </summary>
        [Option('s', "sku", HelpText = "Sku name.", Required = true)]
        public string SkuName { get; set; }

        protected override void ExecuteCommand(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            if (string.IsNullOrEmpty(SkuName))
            {
                throw new InvalidOperationException("SkuName was null or empty.");
            }

            var skuCatalog = services.GetRequiredService<ISkuCatalog>();
            var sku = skuCatalog.CloudEnvironmentSkus.Values.Where(t => t.SkuName == SkuName).Single();

            WriteProperty(sku, stdout);
        }

        protected abstract void WriteProperty(ICloudEnvironmentSku sku, TextWriter stdout);
    }
}
