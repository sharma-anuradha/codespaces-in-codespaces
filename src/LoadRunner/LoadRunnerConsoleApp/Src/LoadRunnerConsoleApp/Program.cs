// <copyright file="Program.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.LoadRunnerConsoleApp.Providers;
using Microsoft.VsSaaS.Services.CloudEnvironments.LoadRunnerConsoleApp.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.LoadRunnerConsoleApp.Repository.AzureCosmosDb;
using Microsoft.VsSaaS.Services.CloudEnvironments.LoadRunnerConsoleApp.Repository.HttpClient;
using Microsoft.VsSaaS.Services.CloudEnvironments.LoadRunnerConsoleApp.Repository.HttpClient.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.LoadRunnerConsoleApp.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LoadRunnerConsoleApp
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
        /// <returns>Resulting task.</returns>
        public static async Task Main(string[] args)
        {
            var collection = new ServiceCollection();

            // Setup config files
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.secrets.json", optional: true);

            if (Debugger.IsAttached)
            {
                builder.AddJsonFile("appsettings.local.json", optional: true);
            }

            builder.AddEnvironmentVariables();

            var configuration = builder.Build();

            // Conig setup
            var appSettingsConfiguration = configuration.GetSection("AppSettings");
            var appSettings = appSettingsConfiguration.Get<AppSettings>();
            collection.AddSingleton(appSettings);

            // Logging setup
            var loggingBaseValues = new LoggingBaseValues
            {
                ServiceName = "load-runer",
                CommitId = appSettings.GitCommit,
            };
            collection.AddTransient<IDiagnosticsLogger>(sp =>
            {
                var loggerFactory = sp.GetService<IDiagnosticsLoggerFactory>();
                var logValueSet = sp.GetService<LogValueSet>();

                return loggerFactory.New(logValueSet);
            });
            collection.AddLoggingBaseValues(loggingBaseValues);

            // Adding developer personal stamp settings and resource name builder.
            var developerPersonalStampSettings = new DeveloperPersonalStampSettings(appSettings.DeveloperPersonalStamp, appSettings.DeveloperAlias);
            collection.AddSingleton(developerPersonalStampSettings);
            collection.AddSingleton<IResourceNameBuilder, ResourceNameBuilder>();

            // Register Http Client Respositories
            collection.AddSingleton<ICurrentUserHttpClientProvider, CurrentUserHttpClientProvider>();
            collection.AddSingleton<IEnvironementsRepository, HttpClientEnvironementsRepository>();

            // Register Document Db Repositories
            collection.AddDocumentDbCollection<ResourcePoolStateSnapshotRecord, IResourcePoolStateSnapshotRepository, CosmosDbResourcePoolStateSnapshotRepository>(
                CosmosDbResourcePoolStateSnapshotRepository.ConfigureOptions);
            collection.AddDocumentDbCollection<ResourcePoolSettingsRecord, IResourcePoolSettingsRepository, CosmosDbResourcePoolSettingsRepository>(
                CosmosDbResourcePoolSettingsRepository.ConfigureOptions);
            collection.AddDocumentDbClientProvider(options =>
            {
                options.ConnectionMode = Microsoft.Azure.Documents.Client.ConnectionMode.Direct;
                options.ConnectionProtocol = Microsoft.Azure.Documents.Client.Protocol.Tcp;
                options.HostUrl = appSettings.Database.HostUrl;
                options.AuthKey = appSettings.Database.AuthKey;
                options.DatabaseId = new ResourceNameBuilder(developerPersonalStampSettings).GetCosmosDocDBName(appSettings.Database.DatabaseId);
                options.PreferredLocation = appSettings.Database.PreferredLocation;
                options.UseMultipleWriteLocations = false;
            });

            // Supporting types
            collection.AddSingleton<IHealthProvider, HealthProvider>();
            collection.AddSingleton<ICurrentUserProvider, CurrentUserProvider>();
            collection.AddSingleton<ITaskHelper, TaskHelper>();

            // Main test runner types
            collection.AddSingleton<TestLoadRunner>();
            collection.AddSingleton<TestArchiveRunner>();

            // Start running actual load test
            var serviceProvider = collection.BuildServiceProvider();

            // Runs test
            // var testRunner = serviceProvider.GetService<TestLoadRunner>();
            var testRunner = serviceProvider.GetService<TestArchiveRunner>();
            var logger = serviceProvider.GetService<IDiagnosticsLogger>();

            await testRunner.RunAsync(logger);

            serviceProvider.Dispose();
        }
    }
}
