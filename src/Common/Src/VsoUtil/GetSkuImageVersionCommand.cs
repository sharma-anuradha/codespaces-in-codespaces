// <copyright file="GetSkuImageVersionCommand.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Management.Cdn.Fluent.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil
{
    /// <summary>
    /// Gets the specified SKU's image version.
    /// </summary>
    [Verb("getskuimageversion", HelpText = "Gets the specified SKU's image version.")]
    public class GetSkuImageVersionCommand : CommandBase
    {
        /// <summary>
        /// Gets or sets the name of the sku whose image version to return.
        /// </summary>
        [Option('s', "sku", HelpText = "Sku name.", Required = true)]
        public string SkuName { get; set; }

        /// <inheritdoc/>
        protected override void ExecuteCommand(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            Execute(services, stdout);
        }

        private void Execute(IServiceProvider services, TextWriter stdout)
        {
            if (string.IsNullOrEmpty(SkuName))
            {
                throw new InvalidOperationException("SkuName was null or empty.");
            }

            var skuCatalog = services.GetRequiredService<ISkuCatalog>();
            var sku = skuCatalog.CloudEnvironmentSkus.Values.Where(t => t.SkuName == SkuName).Single();
            stdout.WriteLine(sku.ComputeImage.DefaultImageVersion);
        }
    }
}
