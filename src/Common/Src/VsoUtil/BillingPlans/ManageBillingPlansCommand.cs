// <copyright file="ManageBillingPlansCommand.cs" company="Microsoft">
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
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil.PrivatePreview;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil.BillingPlans
{
    /// <summary>
    /// Tool to create billing plans in Codespace.
    /// </summary>
    /// </summary>
    [Verb("managebillingplans", HelpText = "Onboard billing plans..")]
    public class ManageBillingPlansCommand : ManageDatabaseCommandBase
    {
        private const string DatabaseSettingsFileName = "appsettings.codespaces.db.json";

        /// <summary>
        /// Gets or sets the target Codespace environment (ppe/prod).
        /// </summary>
        [Option('e', "environment", Default = "dev", HelpText = "Onboard plans in codespace db. Valid options are: prod and ppe.")]
        public string TargetEnvironment { get; set; }

        /// <summary>
        /// Gets or sets the input file path for the billing plans to onboard.
        /// </summary>
        [Option('a', "add", Default = null, HelpText = "Input text file with the plans to onboard/add. File should contain only one billing plan per line.")]
        public string OnboardInputFile { get; set; }

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

                stdout.WriteLine($"Target environment: {TargetEnvironment}");
                stdout.WriteLine($"Onboard Input file: {OnboardInputFile}");
                stdout.WriteLine($"Dry-run: {DryRun}");
                stdout.WriteLine($"-----------------------------------------------");

                // Process add and remove
                await AddBillingPlansAsync(container, stdout, stderr);
            }
            catch (Exception ex)
            {
                stderr.WriteLine($"Failed to process billing plans.\n{ex.Message}");
                stderr.WriteLine($"Failed to process billing plans.\n{ex.StackTrace}");
            }
        }

        private async Task AddBillingPlansAsync(Container container, TextWriter stdout, TextWriter stderr)
        {
            if (string.IsNullOrWhiteSpace(OnboardInputFile))
            {
                stdout.WriteLine("No plans found to onboard.");
                return;
            }

            var resourceIds = GetBillingPlansFromFile(OnboardInputFile, stdout, stderr);
            stdout.WriteLine($"\n Found {resourceIds.Count()} plans for the codepspaces.");
            int failedCount = 0;
            int skippedCount = 0;
            int createdCount = 0;

            foreach (var resourceId in resourceIds)
            {
                stdout.WriteLine($"\n resourceId {resourceId} of the codepspaces.");
                TryParse(resourceId, stdout, out var planInfo);

                Requires.NotNull(planInfo, nameof(planInfo));
                Requires.NotNullOrEmpty(planInfo.Name, nameof(planInfo.Name));
                Requires.NotNullOrEmpty(planInfo.Subscription, nameof(planInfo.Subscription));
                Requires.NotNullOrEmpty(planInfo.ResourceGroup, nameof(planInfo.ResourceGroup));
                Requires.NotNullOrEmpty(planInfo.Location.ToString(), nameof(planInfo.Location));

                QueryDefinition query = new QueryDefinition(
                    $"select * from c where c.plan.name = @name " +
                    $"and c.plan.subscription = @subscription " +
                    $"and c.plan.resourceGroup = @resourceGroup " +
                    $"and c.plan.location = @location " +
                    $"and c.plan.providerNamespace = @providerNamespace")
                    .WithParameter("@name", planInfo.Name)
                    .WithParameter("@subscription", planInfo.Subscription)
                    .WithParameter("@resourceGroup", planInfo.ResourceGroup)
                    .WithParameter("@location", planInfo.Location.ToString())
                    .WithParameter("@providerNamespace", planInfo.ProviderNamespace);

                var queryIterator = container.GetItemQueryIterator<VsoPlan>(query);

                if (queryIterator.HasMoreResults)
                {
                    var queryResponse = await queryIterator.ReadNextAsync();
                    var record = queryResponse.Resource.FirstOrDefault();
                    var action = string.Empty;

                    if (record == null)
                    {
                        var statusCode = default(HttpStatusCode);
                        var request = new VsoPlan()
                        {
                            Id = Guid.NewGuid().ToString(),
                            Plan = planInfo,
                            Partner = Plans.Contracts.Partner.GitHub,
                        };

                        await DoWithDryRun(async () =>
                        {
                            var response = await container.CreateItemAsync(request);
                            statusCode = response.StatusCode;
                        });

                        if (statusCode == HttpStatusCode.Created)
                        {
                            WriteOutPut($"\n {planInfo} - Added, StatusCode : {statusCode}", MessageColorConstants.Add, stdout);
                            createdCount++;
                        }
                        else
                        {
                            WriteOutPut($"\n {planInfo} - Error, StatusCode : {statusCode}", MessageColorConstants.Skip, stdout);
                            failedCount++;
                        }
                    }
                    else
                    {
                        WriteOutPut($"\n {planInfo} - Already exist in the specificed resourceGroup & location", MessageColorConstants.Update, stdout);
                        skippedCount++;
                    }
                }
            }

            WriteOutPut($"\n Total plans {resourceIds.Count()} \n successfully created {createdCount} plans \n error occured for {failedCount} plans \n skipped {skippedCount} plans since they already exist", MessageColorConstants.Update, stdout);
        }

        private IEnumerable<string> GetBillingPlansFromFile(string fileName, TextWriter stdout, TextWriter stderr)
        {
            if (File.Exists(fileName))
            {
                var plans = File.ReadAllLines(fileName);
                return plans.Select(x => x.Trim());
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                stderr.WriteLine($"Input file not found {fileName}");
                Console.ResetColor();
                return Enumerable.Empty<string>();
            }
        }

        private bool TryParse(string resourceId, TextWriter stdout, out VsoPlanInfo plan)
        {
            Requires.NotNullOrEmpty(resourceId, nameof(resourceId));

            resourceId = resourceId.Split('?')[0].Replace("/api/v1", string.Empty);
            var result = VsoPlanInfo.TryParse(resourceId, out plan);
            var location = plan.ResourceGroup.Split('-')[0];

            result &= Enum.TryParse(typeof(AzureLocation), location, out var locationEnum);

            plan.Location = (AzureLocation)locationEnum;

            stdout.WriteLine($"\n Name : {plan.Name} ");
            stdout.WriteLine($"\n Location : {plan.Location} ");
            stdout.WriteLine($"\n Subscription : {plan.Subscription} ");
            stdout.WriteLine($"\n ResourceGroup : {plan.ResourceGroup} ");
            stdout.WriteLine($"\n ProviderNamespace : {plan.ProviderNamespace} ");

            return result;
        }
    }
}
