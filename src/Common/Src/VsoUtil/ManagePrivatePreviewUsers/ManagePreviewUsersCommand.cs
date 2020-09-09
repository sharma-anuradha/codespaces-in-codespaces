// <copyright file="ManagePreviewUsersCommand.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Azure.Cosmos;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil;
using Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil.Models.PrivatePreview;
using Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil.PrivatePreview;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil
{
    /// <summary>
    /// Onboard / Offboard Private Preview users.
    /// </summary>
    [Verb("managepreviewusers", HelpText = "Onboard / Offboard Private Preview users..")]
    public class ManagePreviewUsersCommand : ManageDatabaseCommandBase
    {
        private const string DatabaseSettingsFileName = "appsettings.privatepreview.json";

        /// <summary>
        /// Default preview key.
        /// </summary>
        public const string DefaultPrivatePreviewSkuKey = "vsonline.windowsskupreview";

        /// <summary>
        /// Gets or sets the target liveshare environment (ppe/prod).
        /// </summary>
        [Option('t', "targetenvironment", Default = "prod", HelpText = "Environment to onboard the users to. Valid options are: prod and ppe.")]
        public string TargetEnvironment { get; set; }

        /// <summary>
        /// Gets or sets the input file path for the users to onboard.
        /// </summary>
        [Option('a', "add", Default = null, HelpText = "Input text file with the emails to onboard/add. File should contain only one email address per line.")]
        public string OnboardInputFile { get; set; }

        /// <summary>
        /// Gets or sets the input file path for the users to onboard.
        /// </summary>
        [Option('r', "remove", Default = null, HelpText = "Input text file with the emails to offboard/remove. File should contain only one email address per line.")]
        public string OffboardInputFile { get; set; }

        /// <summary>
        /// Gets or sets the private preview sku key.
        /// </summary>
        [Option('k', "key", Default = DefaultPrivatePreviewSkuKey, HelpText = "Private preview sku key. Default is:" + DefaultPrivatePreviewSkuKey)]
        public string PrivatePreviewSkuKey { get; set; }

        /// <inheritdoc/>
        protected override void ExecuteCommand(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            ExecuteCommandAsync(stdout, stderr).Wait();
        }

        private async Task ExecuteCommandAsync(TextWriter stdout, TextWriter stderr)
        {
            try
            {
                var aadToken = await ExecuteAuthenticationAsync(stdout, stderr);
                var container = await GetDatabaseContainerAsync(DatabaseSettingsFileName, TargetEnvironment, aadToken, stderr);

                OnboardInputFile = GetFullFilePath(OnboardInputFile);
                OffboardInputFile = GetFullFilePath(OffboardInputFile);

                stdout.WriteLine($"Target environment: {TargetEnvironment}");
                stdout.WriteLine($"Onboard Input file: {OnboardInputFile}");
                stdout.WriteLine($"Offboard Input file: {OffboardInputFile}");
                stdout.WriteLine($"Preview Sku key: {PrivatePreviewSkuKey}");
                stdout.WriteLine($"Dry-run: {DryRun}");
                stdout.WriteLine($"-----------------------------------------------");

                // Process add and remove
                await ProcessAddAsync(container, PrivatePreviewSkuKey ?? DefaultPrivatePreviewSkuKey, stdout, stderr);
                await ProcessRemoveAsync(container, PrivatePreviewSkuKey ?? DefaultPrivatePreviewSkuKey, stdout, stderr);
            }
            catch (Exception ex)
            {
                stderr.WriteLine($"Failed to process private preview users.\n{ex.Message}");
            }
        }

        private async Task ProcessRemoveAsync(Container container, string previewKey, TextWriter stdout, TextWriter stderr)
        {
            if (string.IsNullOrWhiteSpace(OffboardInputFile))
            {
                stdout.WriteLine("No emails to offboard.");
                return;
            }

            var emailIds = GetEmailIdsFromFile(OffboardInputFile, stdout, stderr);
            stdout.WriteLine($"\nRemoving {emailIds.Count()} email ids from the private preview of {previewKey}.");

            foreach (var email in emailIds)
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    continue;
                }

                QueryDefinition query = new QueryDefinition($"select * from c where c.id = @email").WithParameter("@email", email);
                var queryIterator = container.GetItemQueryIterator<ProfileProgram>(query);
                if (queryIterator.HasMoreResults)
                {
                    var queryResponse = await queryIterator.ReadNextAsync();
                    var record = queryResponse.Resource.FirstOrDefault();

                    if (record == null)
                    {
                        WriteOutPut($"{email} - Skip (NotFound)", MessageColorConstants.Skip, stdout);
                        continue;
                    }

                    if (record.Items != null)
                    {
                        if (record.GetItem<bool>(previewKey))
                        {
                            if (record.Items.Count() == 1)
                            {
                                // Delete record
                                var statusCode = default(HttpStatusCode);
                                await DoWithDryRun(async () =>
                                {
                                    var response = await container.DeleteItemAsync<ProfileProgram>(record.Id, PartitionKey.None);
                                    statusCode = response.StatusCode;
                                });

                                WriteOutPut($"{email} - Delete, StatusCode : {statusCode}", MessageColorConstants.Delete, stdout);

                                continue;
                            }

                            record.Items[previewKey] = false;
                        }
                        else
                        {
                            WriteOutPut($"{email} - Skip", MessageColorConstants.Skip, stdout);
                            continue;
                        }
                    }
                    else
                    {
                        // Delete record
                        var response = await container.DeleteItemAsync<ProfileProgram>(record.Id, PartitionKey.None);
                        WriteOutPut($"{email} - Delete, StatusCode : {response.StatusCode}", MessageColorConstants.Delete, stdout);
                        continue;
                    }

                    var updateStatusCode = default(HttpStatusCode);
                    await DoWithDryRun(async () =>
                    {
                        var response = await container.UpsertItemAsync<ProfileProgram>(record);
                        updateStatusCode = response.StatusCode;
                    });

                    WriteOutPut($"{email} - Update, StatusCode : {updateStatusCode}", MessageColorConstants.Update, stdout);
                }
            }
        }

        private async Task ProcessAddAsync(Container container, string previewKey, TextWriter stdout, TextWriter stderr)
        {
            if (string.IsNullOrWhiteSpace(OnboardInputFile))
            {
                stdout.WriteLine("No emails to onboard.");
                return;
            }

            var emailIds = GetEmailIdsFromFile(OnboardInputFile, stdout, stderr);
            stdout.WriteLine($"\nAdding {emailIds.Count()} email ids for the private preview of {previewKey}.");

            foreach (var email in emailIds)
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    continue;
                }

                QueryDefinition query = new QueryDefinition($"select * from c where c.id = @email").WithParameter("@email", email);
                var queryIterator = container.GetItemQueryIterator<ProfileProgram>(query);
                if (queryIterator.HasMoreResults)
                {
                    var queryResponse = await queryIterator.ReadNextAsync();
                    var record = queryResponse.Resource.FirstOrDefault();
                    var action = string.Empty;

                    if (record != null)
                    {
                        if (record.Items == null)
                        {
                            record.Items = new Dictionary<string, object>();
                        }
                        else
                        {
                            if (record.GetItem<bool>(previewKey))
                            {
                                WriteOutPut($"{email} - Skip", MessageColorConstants.Skip, stdout);
                                continue;
                            }
                        }

                        record.Items[previewKey] = true;

                        var statusCode = default(HttpStatusCode);
                        await DoWithDryRun(async () =>
                        {
                            var response = await container.UpsertItemAsync<ProfileProgram>(record);
                            statusCode = response.StatusCode;
                        });

                        WriteOutPut($"{email} - Update, StatusCode : {statusCode}", MessageColorConstants.Update, stdout);
                    }
                    else
                    {
                        record = new ProfileProgram()
                        {
                            Id = email,
                            Items = new Dictionary<string, object>
                            {
                                { previewKey, true },
                            },
                        };

                        var statusCode = default(HttpStatusCode);
                        await DoWithDryRun(async () =>
                        {
                            var response = await container.CreateItemAsync<ProfileProgram>(record);
                            statusCode = response.StatusCode;
                        });

                        WriteOutPut($"{email} - Add, StatusCode : {statusCode}", MessageColorConstants.Add, stdout);
                    }
                }
            }
        }

        private IEnumerable<string> GetEmailIdsFromFile(string fileName, TextWriter stdout, TextWriter stderr)
        {
            if (File.Exists(fileName))
            {
                var emails = File.ReadAllLines(fileName);
                return emails.Select(x => x.ToLowerInvariant().Trim());
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                stderr.WriteLine($"Input file not found {fileName}");
                Console.ResetColor();
                return Enumerable.Empty<string>();
            }
        }
    }
}
