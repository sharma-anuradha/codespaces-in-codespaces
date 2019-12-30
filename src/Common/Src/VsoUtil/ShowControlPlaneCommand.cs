// <copyright file="ShowControlPlaneCommand.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.IO;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil
{
    /// <summary>
    /// The show control-plan info verb.
    /// </summary>
    [Verb("controlplane", HelpText = "Show the control plane info.")]
    public class ShowControlPlaneCommand : CommandBase
    {
        /// <inheritdoc/>
        protected override void ExecuteCommand(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            var controlPlaneInfo = services.GetRequiredService<IControlPlaneInfo>();
            var info = JsonSerializeObject(controlPlaneInfo);
            stdout.WriteLine(info);
        }
    }
}
