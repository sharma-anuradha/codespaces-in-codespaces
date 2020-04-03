// <copyright file="ShowDbAccountInfoCommand.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil
{
    /// <summary>
    /// Shows details about the Cosmos DB account and endpoints used by the current environment config.
    /// </summary>
    [Verb("cosmosdb-info", HelpText = "Show Cosmos DB account info.")]
    public class ShowDbAccountInfoCommand : CommandBase
    {
        /// <inheritdoc/>
        protected override void ExecuteCommand(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            ExecuteCommandAsync(services, stdout, stderr).Wait();
        }

        private async Task ExecuteCommandAsync(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            var appSettings = services.GetRequiredService<AppSettingsBase>();
            var healthProvider = services.GetRequiredService<IHealthProvider>();
            var loggerFactory = services.GetRequiredService<IDiagnosticsLoggerFactory>();
            var logValues = services.GetRequiredService<LogValueSet>();
            var controlPlaneInfo = services.GetRequiredService<IControlPlaneInfo>();
            var controlPlaneAzureResourceAccessor = services.GetRequiredService<IControlPlaneAzureResourceAccessor>();
            var resourceNameBuilder = services.GetRequiredService<IResourceNameBuilder>();

            var json = JsonSerializer.Create(new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
            });

            async Task<CosmosDbAccountInfo> GetDbAccountInfo(string hostUrl, string authKey)
            {
                var stampClientOptions = Options.Create(new DocumentDbClientOptions
                {
                    HostUrl = hostUrl,
                    AuthKey = authKey,
                    DatabaseId = resourceNameBuilder.GetCosmosDocDBName(appSettings.AzureCosmosDbDatabaseId),
                    PreferredLocation = controlPlaneInfo.Stamp.Location.ToString(),
                    UseMultipleWriteLocations = true,
                });
                var dbClientProvider = new DocumentDbClientProvider(stampClientOptions, healthProvider, loggerFactory, logValues);
                var dbClient = await dbClientProvider.GetClientAsync();

                var dbAccount = await dbClient.GetDatabaseAccountAsync();

                return new CosmosDbAccountInfo
                {
                    Id = dbAccount.Id,
                    ServiceEndpoint = dbClient.ServiceEndpoint.AbsoluteUri,
                    ReadEndpoint = dbClient.ReadEndpoint.AbsoluteUri,
                    ReadableEndpoints = dbAccount.ReadableLocations.Select((l) => l.DatabaseAccountEndpoint).ToArray(),
                    WriteEndpoint = dbClient.WriteEndpoint.AbsoluteUri,
                    WritableEndpoints = dbAccount.WritableLocations.Select((l) => l.DatabaseAccountEndpoint).ToArray(),
                };
            }

            var (instanceHostUrl, instanceAuthKey) = controlPlaneAzureResourceAccessor.GetInstanceCosmosDbAccountAsync().Result;
            stdout.WriteLine("Instance CosmosDB");
            json.Serialize(stdout, await GetDbAccountInfo(instanceHostUrl, instanceAuthKey));
            stdout.WriteLine();
            stdout.WriteLine();
            var (stampHostUrl, stampAuthKey) = controlPlaneAzureResourceAccessor.GetStampCosmosDbAccountAsync().Result;
            stdout.WriteLine("Stamp CosmosDB");
            json.Serialize(stdout, await GetDbAccountInfo(stampHostUrl, stampAuthKey));
        }

        private class CosmosDbAccountInfo
        {
            public string Id { get; set; }

            public string ServiceEndpoint { get; set; }

            public string ReadEndpoint { get; set; }

            public string[] ReadableEndpoints { get; set; }

            public string WriteEndpoint { get; set; }

            public string[] WritableEndpoints { get; set;  }
        }
    }
}
