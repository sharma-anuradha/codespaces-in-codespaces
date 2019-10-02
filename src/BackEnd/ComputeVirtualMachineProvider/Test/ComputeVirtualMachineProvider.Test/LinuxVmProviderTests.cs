using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Test
{
    public class LinuxVmProviderTests : IClassFixture<AzureVmProviderTestsBase>
    {
        private const int TargetVmCount = 1;
        private AzureVmProviderTestsBase testContext;
        public LinuxVmProviderTests(AzureVmProviderTestsBase data)
        {
            this.testContext = data;
        }

        [Trait("Category", "IntegrationTest")]
        [Fact]
        public async Task<AzureResourceInfo> Create_Compute_Ok()
        {
            var logger = new DefaultLoggerFactory().New();
            AzureClientFactoryMock clientFactory = new AzureClientFactoryMock(testContext.AuthFilePath);
            var azureDeploymentManager = new LinuxVirtualMachineManager(
                clientFactory,
                new MockControlPlaneAzureResourceAccessor(clientFactory));

            var computeProvider = new VirtualMachineProvider(new[] { azureDeploymentManager });
            Guid subscriptionId = this.testContext.SubscriptionId;
            AzureLocation location = testContext.Location;
            string rgName = testContext.ResourceGroupName;

            VirtualMachineProviderCreateInput input = new VirtualMachineProviderCreateInput()
            {
                VMToken = Guid.NewGuid().ToString(),
                AzureVmLocation = location,
                AzureResourceGroup = rgName,
                AzureSubscription = subscriptionId,
                AzureVirtualMachineImage = "Canonical.UbuntuServer.18.04-LTS.latest",
                AzureSkuName = "Standard_F4s_v2",
                ResourceTags = new Dictionary<string, string> {
                    {"ResourceTag", "GeneratedFromTest"},
                },
                ComputeOS = ComputeOS.Linux,
                VmAgentBlobUrl = testContext.Config["VM_AGENT_SOURCE_URL"],
                ResourceId = Guid.NewGuid().ToString(),
                FrontDnsHostName = "fontend.service.com",
            };

            var timerCreate = Stopwatch.StartNew();
            VirtualMachineProviderCreateResult createResult = await computeProvider.CreateAsync(input, logger);
            timerCreate.Stop();
            Console.WriteLine($"Time taken to begin create VM {timerCreate.Elapsed.TotalSeconds}");
            var timerWait = Stopwatch.StartNew();
            Assert.NotNull(createResult);
            Assert.Equal(OperationState.InProgress, createResult.Status);
            NextStageInput createDeploymentStatusInput = createResult.NextInput.ContinuationToken.ToNextStageInput();
            Assert.NotNull(createDeploymentStatusInput);
            Assert.NotNull(createDeploymentStatusInput.TrackingId);
            Assert.Equal(rgName, createDeploymentStatusInput.AzureResourceInfo.ResourceGroup);
            Assert.Equal(subscriptionId, createDeploymentStatusInput.AzureResourceInfo.SubscriptionId);
            VirtualMachineProviderCreateResult statusCheckResult = default;
            do
            {
                await Task.Delay(500);
                input = input.BuildNextInput(createResult.NextInput.ContinuationToken);
                statusCheckResult = await computeProvider.CreateAsync(input, logger);
                Assert.NotNull(statusCheckResult);
                Assert.True(statusCheckResult.Status.Equals(OperationState.InProgress) || statusCheckResult.Status.Equals(OperationState.Succeeded));
                if (statusCheckResult.Status.Equals(OperationState.InProgress))
                {
                    NextStageInput statusCheckdeploymentStatusToken = statusCheckResult.NextInput.ContinuationToken.ToNextStageInput();
                    Assert.NotNull(statusCheckdeploymentStatusToken);
                }
            } while (statusCheckResult.Status.Equals(OperationState.InProgress));
            timerWait.Stop();
            Console.WriteLine($"Time taken to create VM {timerWait.Elapsed.TotalSeconds}");
            return statusCheckResult.AzureResourceInfo;
        }

        [Trait("Category", "IntegrationTest")]
        [Fact]
        public async Task Create_Multiple_Compute_Ok()
        {
            var logger = new DefaultLoggerFactory().New();
            AzureClientFactoryMock clientFactory = new AzureClientFactoryMock(testContext.AuthFilePath);
            var azureDeploymentManager = new LinuxVirtualMachineManager(
                clientFactory,
                new MockControlPlaneAzureResourceAccessor(clientFactory));

            var computeProvider = new VirtualMachineProvider(new[] { azureDeploymentManager });
            Guid subscriptionId = this.testContext.SubscriptionId;
            AzureLocation location = testContext.Location;
            string rgName = testContext.ResourceGroupName;

            VirtualMachineProviderCreateInput input = new VirtualMachineProviderCreateInput()
            {
                VMToken = Guid.NewGuid().ToString(),
                AzureVmLocation = location,
                AzureResourceGroup = rgName,
                AzureSubscription = subscriptionId,
                AzureVirtualMachineImage = "Canonical.UbuntuServer.18.04-LTS.latest",
                AzureSkuName = "Standard_F4s_v2",
                ComputeOS = ComputeOS.Linux,
            };

            var timerCreate = Stopwatch.StartNew();
            VirtualMachineProviderCreateResult[] initiateVmCreationList = await Task.WhenAll(Enumerable.Range(0, TargetVmCount).Select(x => computeProvider.CreateAsync(input, logger)));
            timerCreate.Stop();
            Console.WriteLine($"Time taken to begin create 10 VMs {timerCreate.Elapsed.TotalSeconds}");

            var timerWait = Stopwatch.StartNew();
            VirtualMachineProviderCreateResult[] vmStatus = await Task.WhenAll(initiateVmCreationList.Select(x => WaitForVMCreation(computeProvider, input, logger)));
            timerWait.Stop();
            Console.WriteLine($"Time taken to create VM {timerWait.Elapsed.TotalSeconds}");
        }

        [Trait("Category", "IntegrationTest")]
        [Fact]
        public async Task Start_Compute_Ok()
        {
            var logger = new DefaultLoggerFactory().New();
            AzureClientFactoryMock clientFactory = new AzureClientFactoryMock(testContext.AuthFilePath);
            var azureDeploymentManager = new LinuxVirtualMachineManager(
                clientFactory,
                new MockControlPlaneAzureResourceAccessor(clientFactory));
            var prereqexsit = ValidatePrereqConfig("startCompute");
            Assert.True(prereqexsit, "Failed, as required config is not set.");
  
            var computeProvider = new VirtualMachineProvider(new[] { azureDeploymentManager });
            var fileShareInfo = new ShareConnectionInfo(this.testContext.Config["FILE_STORE_ACCOUNT"],
                                                       this.testContext.Config["FILE_STORE_KEY"],
                                                       "cloudenvdata",
                                                       "dockerlib");

            var vmResourceInfo = (await this.Create_Compute_Ok());
            var startComputeInput = new VirtualMachineProviderStartComputeInput(
                vmResourceInfo,
                fileShareInfo,
                new Dictionary<string, string>()
                   {
                        { "SESSION_ID", this.testContext.Config["SESSION_ID"] },
                        { "SESSION_TOKEN", this.testContext.Config["SESSION_TOKEN"] },
                        { "SESSION_CALLBACK",this.testContext.Config["SESSION_CALLBACK"] },
                   },
                ComputeOS.Linux,
                this.testContext.Location,
                null);

            var timerStartCompute = Stopwatch.StartNew();
            VirtualMachineProviderStartComputeResult startComputeResult = await computeProvider.StartComputeAsync(startComputeInput, logger);
            timerStartCompute.Stop();
            Console.WriteLine($"Time taken to allocate VM {timerStartCompute.Elapsed.TotalSeconds}");
            Assert.Equal(OperationState.Succeeded, startComputeResult.Status);
        }

        [Trait("Category", "IntegrationTest")]
        [Fact]
        public async Task Delete_Compute_Ok()
        {
            var logger = new DefaultLoggerFactory().New();
            var deleteTimer = Stopwatch.StartNew();
            AzureClientFactoryMock clientFactory = new AzureClientFactoryMock(testContext.AuthFilePath);
            var azureDeploymentManager = new LinuxVirtualMachineManager(
                clientFactory,
                new MockControlPlaneAzureResourceAccessor(clientFactory));
            var computeProvider = new VirtualMachineProvider(new[] { azureDeploymentManager });

            var vmResourceInfo = (await this.Create_Compute_Ok());
            var input = new VirtualMachineProviderDeleteInput
            {
                AzureResourceInfo = vmResourceInfo,
                AzureVmLocation = AzureLocation.WestUs2,
                ComputeOS = ComputeOS.Linux,
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
                    NextStageInput statusCheckdeploymentStatusToken = deleteStatusCheckResult.NextInput.ContinuationToken.ToNextStageInput();
                    Assert.NotNull(statusCheckdeploymentStatusToken);
                }
            } while (deleteStatusCheckResult.Status.Equals(OperationState.InProgress));
            timerWait.Stop();
            Console.WriteLine($"Time taken to start environment on VM {timerWait.Elapsed.TotalSeconds}");
        }

        private bool ValidatePrereqConfig(string scenario)
        {
            switch (scenario)
            {
                case "startCompute":
                    return !string.IsNullOrEmpty(this.testContext.Config["FILE_STORE_ACCOUNT"])
                            && !string.IsNullOrEmpty(this.testContext.Config["FILE_STORE_KEY"])
                            && !string.IsNullOrEmpty(this.testContext.Config["SESSION_ID"])
                            && !string.IsNullOrEmpty(this.testContext.Config["SESSION_TOKEN"])
                            && !string.IsNullOrEmpty(this.testContext.Config["SESSION_CALLBACK"]);
                default:
                    return true;
            }
        }

        private static async Task<VirtualMachineProviderCreateResult> WaitForVMCreation(VirtualMachineProvider computeProvider, VirtualMachineProviderCreateInput input, IDiagnosticsLogger logger)
        {
            VirtualMachineProviderCreateResult statusCheckResult = default;
            do
            {
                await Task.Delay(500);
                statusCheckResult = await computeProvider.CreateAsync(input, logger);
                Assert.NotNull(statusCheckResult);
                Assert.True(statusCheckResult.Status.Equals(OperationState.InProgress) || statusCheckResult.Status.Equals(OperationState.Succeeded));
                if (statusCheckResult.Status.Equals(OperationState.InProgress))
                {
                    NextStageInput statusCheckdeploymentStatusToken = statusCheckResult.NextInput.ContinuationToken.ToNextStageInput();
                    Assert.NotNull(statusCheckdeploymentStatusToken);
                }
            } while (statusCheckResult.Status.Equals(OperationState.InProgress));
            return statusCheckResult;
        }
    }
}