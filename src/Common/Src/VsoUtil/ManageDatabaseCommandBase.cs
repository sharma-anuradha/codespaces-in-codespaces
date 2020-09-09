// <copyright file="ManageDatabaseCommandBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Identity.Client;
using Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil.Models.PrivatePreview;
using Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil.PrivatePreview;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil
{
    /// <summary>
    /// Command options.
    /// </summary>
    public abstract class ManageDatabaseCommandBase : CommandBase
    {
        private const string AadAppsettingsfileName = "appsettings.aad.json";

        /// <summary>
        /// Executes authentication
        /// </summary>
        /// <param name="stdout"> Std out for writing text.</param>
        /// <param name="stderr"> Std err for writing text.</param>
        protected async Task<string> ExecuteAuthenticationAsync(TextWriter stdout, TextWriter stderr)
        {
            var aadToken = default(string);
            try
            {
                var aadAppSettings = LoadAppSettings<AadAppSettings>(AadAppsettingsfileName);
                aadToken = await AquireAadTokenAsync(aadAppSettings, stdout);
                var azureClient = new AzureManagementHttpClient(aadToken);
                return aadToken;
            }
            catch (Exception ex)
            {
                stderr.WriteLine($"Failed to Authenticate.\n{ex.StackTrace}");
            }

            return aadToken;
        }

        /// <summary>
        /// Get db container
        /// </summary>
        /// <param name="dbSettingsFileName"> Db appsettings file name.</param>
        /// <param name="targetEnviroment"> Environment being targetted .</param>
        /// <param name="aadToken"> AAD Token.</param>
        /// <param name="stderr"> Std err for writing text.</param>
        protected async Task<Container> GetDatabaseContainerAsync(
            string dbSettingsFileName,
            string targetEnviroment,
            string aadToken,
            TextWriter stderr)
        {
            var container = default(Container);
            try
            {
                var appSettings = LoadAppSettings<ManageToolSettings>(dbSettingsFileName);
                var databaseInfo = appSettings.Databases.FirstOrDefault(x => x.Environment == targetEnviroment);

                if (databaseInfo == default)
                {
                    throw new Exception($"Invalid target environment: '{targetEnviroment}'");
                }

                var azureClient = new AzureManagementHttpClient(aadToken);
                var dbCredentials = await azureClient.FetchDatabaseCredentials(databaseInfo.AzureInfo);

                databaseInfo.Credentials = dbCredentials;

                var cosmosClient = new CosmosClient(databaseInfo.Uri, databaseInfo.Credentials.PrimaryMasterKey);

                // Get the database
                var database = cosmosClient.GetDatabase(databaseInfo.DatabaseId);

                // Get the container
                container = database.GetContainer(databaseInfo.ContainerId);
            }
            catch (Exception ex)
            {
                stderr.WriteLine($"Failed to Authenticate.\n{ex.StackTrace}");
            }

            return container;
        }

        /// <summary>
        /// Write output in specified color.
        /// </summary>
        /// <param name="message">Message to be writen.</param>
        /// <param name="consoleColor">Color to be used.</param>
        /// <param name="stdout"> Std out for writing text.</param>
        protected void WriteOutPut(string message, ConsoleColor consoleColor, TextWriter stdout)
        {
            Console.ForegroundColor = consoleColor;
            stdout.WriteLine(message);
            Console.ResetColor();
        }

        /// <summary>
        /// AAD token process.
        /// </summary>
        /// <param name="settings"> Tool settings.</param>
        /// <param name="stdout">Std out for text writer.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected async Task<string> AquireAadTokenAsync(AadAppSettings settings, TextWriter stdout)
        {
            var app = PublicClientApplicationBuilder
              .Create(settings.AzureClientId)
              .WithAuthority(settings.AzureAuthority)
              .WithDefaultRedirectUri()
              .Build();

            var tokenAcquisitionHelper = new PublicAppUsingDeviceCodeFlow(app);
            var authenticationResult = await tokenAcquisitionHelper.AcquireATokenFromCacheOrDeviceCodeFlowAsync(settings.AadScopes, stdout);
            if (authenticationResult != default)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                stdout.WriteLine($"{authenticationResult.Account.Username} successfully signed-in");
                Console.ResetColor();

                return authenticationResult.AccessToken;
            }
            else
            {
                throw new UnauthorizedAccessException("Failed to login to Azure.");
            }
        }

        /// <summary>
        /// Loading the settings from json file.
        /// </summary>
        /// <param name="fileName"> Name of the file to be specified.</param>
        /// <typeparam name="T"> To object we need to desrialize to.</typeparam>
        /// <returns> returns T object.</returns>
        protected T LoadAppSettings<T>(string fileName)
        {
            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), fileName);
            var jsonData = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<T>(jsonData);
        }

        /// <summary>
        /// Get the full file path.
        /// </summary>
        /// <param name="file"> File to be specified.</param>
        /// <returns> The full path of the file.</returns>
        protected string GetFullFilePath(string file)
        {
            if (string.IsNullOrWhiteSpace(file) || Path.IsPathFullyQualified(file))
            {
                return file;
            }

            return Path.GetFullPath(file);
        }
    }
}
