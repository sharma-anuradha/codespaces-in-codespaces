using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine.Strategies;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;
using Moq;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Test
{
    public class LinuxVirtualMachineManagerTests : IClassFixture<AzureComputeProviderTestsBase>
    {
        private readonly AzureComputeProviderTestsBase testContext;
        private const string ComputeVsoAgentImageBlobName = "VSOAgent_linux_3236380.zip";

        public LinuxVirtualMachineManagerTests(AzureComputeProviderTestsBase data)
        {
            testContext = data;
        }

        [Trait("Category", "IntegrationTest")]
        [Fact]
        public async Task LinuxCompute_Create_Start_Delete_Ok()
        {
            var clientFactory = new AzureClientFactory(testContext.SystemCatalog.AzureSubscriptionCatalog);
            var queueProvider = new VirtualMachineQueueProvider(testContext.ResourceAccessor);
            var azureDeploymentManager = new VirtualMachineDeploymentManager(
                clientFactory,
                new Mock<IAzureClientFPAFactory>().Object, // pass mock as its only needed for vnet scenarios.
                queueProvider,
                new List<ICreateVirtualMachineStrategy>() { new CreateLinuxVirtualMachineBasicStrategy(clientFactory, queueProvider) });
            var computeProvider = new VirtualMachineProvider(azureDeploymentManager);

            var vmResourceInfo = await Create_Compute_Ok(computeProvider, ComputeOS.Linux, "Standard_F4s_v2", "Canonical.UbuntuServer.18.04-LTS.latest", ComputeVsoAgentImageBlobName);
            await Start_Compute_Ok(computeProvider, vmResourceInfo);
            await Delete_Compute_Ok(computeProvider, vmResourceInfo, ComputeOS.Linux);
        }

        public async Task<AzureResourceInfo> Create_Compute_Ok(IComputeProvider computeProvider, ComputeOS computeOS, string azureSku, string vmImage, string vsoBlobName)
        {
            var logger = new DefaultLoggerFactory().New();
            var subscriptionId = testContext.SubscriptionId;
            var location = testContext.Location;
            var rgName = testContext.ResourceGroupName;
            var blobUrl = await testContext.GetSrcBlobUrlAsync(vsoBlobName);
            var input = new VirtualMachineProviderCreateInput()
            {
                VMToken = Guid.NewGuid().ToString(),
                AzureVmLocation = location,
                AzureResourceGroup = rgName,
                AzureSubscription = subscriptionId,
                AzureVirtualMachineImage = vmImage,
                AzureSkuName = azureSku,
                ResourceTags = new Dictionary<string, string> {
                    {"ResourceTag", "GeneratedFromTest"},
                },
                ComputeOS = computeOS,
                VmAgentBlobUrl = blobUrl,
                ResourceId = Guid.NewGuid().ToString(),
                FrontDnsHostName = "frontend.service.com",
            };

            var timerCreate = Stopwatch.StartNew();
            var createResult = await computeProvider.CreateAsync(input, logger);
            timerCreate.Stop();
            Console.WriteLine($"Time taken to begin create VM {timerCreate.Elapsed.TotalSeconds}");
            var timerWait = Stopwatch.StartNew();
            Assert.NotNull(createResult);
            Assert.Equal(OperationState.InProgress, createResult.Status);
            var createDeploymentStatusInput = createResult.NextInput.ContinuationToken.ToNextStageInput();
            Assert.NotNull(createDeploymentStatusInput);
            Assert.NotNull(createDeploymentStatusInput.TrackingId);
            Assert.Equal(rgName, createDeploymentStatusInput.AzureResourceInfo.ResourceGroup);
            Assert.Equal(subscriptionId, createDeploymentStatusInput.AzureResourceInfo.SubscriptionId);
            VirtualMachineProviderCreateResult statusCheckResult;
            do
            {
                await Task.Delay(500);
                input = input.BuildNextInput(createResult.NextInput.ContinuationToken);
                statusCheckResult = await computeProvider.CreateAsync(input, logger);
                Assert.NotNull(statusCheckResult);
                Assert.True(
                    statusCheckResult.Status.Equals(OperationState.InProgress) || statusCheckResult.Status.Equals(OperationState.Succeeded),
                    $"Unexpected status: {statusCheckResult.Status} : {statusCheckResult.ErrorReason}");
                if (statusCheckResult.Status.Equals(OperationState.InProgress))
                {
                    var statusCheckdeploymentStatusToken = statusCheckResult.NextInput.ContinuationToken.ToNextStageInput();
                    Assert.NotNull(statusCheckdeploymentStatusToken);
                }
            } while (statusCheckResult.Status.Equals(OperationState.InProgress));
            timerWait.Stop();
            Console.WriteLine($"Time taken to create VM {timerWait.Elapsed.TotalSeconds}");
            return statusCheckResult.AzureResourceInfo;
        }

        internal async Task Start_Compute_Ok(IComputeProvider computeProvider, AzureResourceInfo vmResourceInfo)
        {
            var logger = new DefaultLoggerFactory().New();
            var storageAccountName = GetConfigOrDefault("FILE_STORE_ACCOUNT", "teststorageaccount");
            var storageAccountKey = GetConfigOrDefault("FILE_STORE_KEY", "teststorageaccountkey");

            var fileShareInfo = new ShareConnectionInfo(storageAccountName,
                                                       storageAccountKey,
                                                       "cloudenvdata",
                                                       "dockerlib");

            var startComputeInput = new VirtualMachineProviderStartComputeInput(
                vmResourceInfo,
                fileShareInfo,
                new Dictionary<string, string>()
                   {
                        { "CLOUDENV_ENVIRONMENT_ID", GetConfigOrDefault("CLOUDENV_ENVIRONMENT_ID", "CLOUDENV_ENVIRONMENT_ID") },
                        { "SESSION_ID", GetConfigOrDefault("SESSION_ID","SESSION_ID") },
                        { "SESSION_TOKEN",  GetConfigOrDefault("SESSION_TOKEN","SESSION_TOKEN") },
                        { "SESSION_CASCADE_TOKEN", GetConfigOrDefault("SESSION_CASCADE_TOKEN","SESSION_CASCADE_TOKEN") },
                        { "SESSION_CALLBACK", GetConfigOrDefault("SESSION_CALLBACK","SESSION_CALLBACK") },
                        { "GIT_CONFIG_USER_NAME", GetConfigOrDefault("USER_NAME", "GIT_CONFIG_USER_NAME") },
                        { "GIT_CONFIG_USER_EMAIL",  GetConfigOrDefault("USER_EMAIL","GIT_CONFIG_USER_EMAIL")},
                   },
                new HashSet<UserSecretData>(),
                ComputeOS.Linux,
                testContext.Location,
                "Standard_D4s_v3",
                null);

            var timerStartCompute = Stopwatch.StartNew();
            var startComputeResult = await computeProvider.StartComputeAsync(startComputeInput, logger);
            timerStartCompute.Stop();
            Console.WriteLine($"Time taken to allocate VM {timerStartCompute.Elapsed.TotalSeconds}");
            Assert.Equal(OperationState.Succeeded, startComputeResult.Status);
        }

        internal async Task Delete_Compute_Ok(IComputeProvider computeProvider, AzureResourceInfo vmResourceInfo, ComputeOS computeOS)
        {
            var logger = new DefaultLoggerFactory().New();
            var deleteTimer = Stopwatch.StartNew();
            var input = new VirtualMachineProviderDeleteInput
            {
                AzureResourceInfo = vmResourceInfo,
                AzureVmLocation = testContext.Location,
                ComputeOS = computeOS,
            };

            var deleteResult = await computeProvider.DeleteAsync(input, logger);
            deleteTimer.Stop();
            Console.WriteLine($"Time taken to create VM {deleteTimer.Elapsed.TotalSeconds}");

            var deleteStatusCheckResult = deleteResult;
            var timerWait = Stopwatch.StartNew();
            do
            {
                await Task.Delay(500);
                deleteStatusCheckResult = await computeProvider.DeleteAsync((VirtualMachineProviderDeleteInput)deleteStatusCheckResult.NextInput, logger);
                Assert.NotNull(deleteStatusCheckResult);
                Assert.True(deleteStatusCheckResult.Status.Equals(OperationState.InProgress)
                    || deleteStatusCheckResult.Status.Equals(OperationState.Succeeded));
                if (deleteStatusCheckResult.Status.Equals(OperationState.InProgress))
                {
                    var statusCheckdeploymentStatusToken = deleteStatusCheckResult.NextInput.ContinuationToken.ToNextStageInput();
                    Assert.NotNull(statusCheckdeploymentStatusToken);
                }
            } while (deleteStatusCheckResult.Status.Equals(OperationState.InProgress));
            timerWait.Stop();
            Console.WriteLine($"Time taken to start environment on VM {timerWait.Elapsed.TotalSeconds}");
        }

        private string GetConfigOrDefault(string configKey, string defaultValue)
        {
            var result = testContext.Config["FILE_STORE_ACCOUNT"];
            result = string.IsNullOrEmpty(result) ? defaultValue : defaultValue;
            return result;
        }
    }
}