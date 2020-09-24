// <copyright file="GetSkuVsVersionCommand.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.IO;
using CommandLine;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil
{
    /// <summary>
    /// Gets the specified SKU's VS version.
    /// </summary>
    [Verb("getskuvsversion", HelpText = "Gets the specified SKU's VS version.")]
    public class GetSkuVsVersionCommand : GetSkuPropertyCommandBase
    {
        protected override void WriteProperty(ICloudEnvironmentSku sku, TextWriter stdout)
        {
            stdout.WriteLine(sku.ComputeImage.VsVersion);
        }
    }
}
