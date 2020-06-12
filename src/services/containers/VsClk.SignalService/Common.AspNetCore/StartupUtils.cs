// <copyright file="StartupUtils.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Microsoft.VsCloudKernel.SignalService
{
    public static class StartupUtils
    {
        public static void ConfigureAppConfiguration(
            WebHostBuilderContext context,
            IConfigurationBuilder config,
            string[] args)
        {
            config.SetBasePath(context.HostingEnvironment.ContentRootPath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
#if DEBUG
            .AddJsonFile("appsettings.Debug.json", optional: true, reloadOnChange: true)
#endif
            .AddEnvironmentVariables()
            .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddCommandLine(args);
        }
    }
}
