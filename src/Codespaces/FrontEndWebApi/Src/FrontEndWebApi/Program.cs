// <copyright file="Program.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi
{
    /// <summary>
    /// Entry point for the service.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Runs the web server.
        /// </summary>
        /// <param name="args">Arguments to change the way the host is built.
        /// Not usually needed.</param>
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        private static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
