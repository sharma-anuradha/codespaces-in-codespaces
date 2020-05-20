// <copyright file="VirtualMachineDeploymentManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine.Strategies;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Abstractions;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine
{
    /// <summary>
    /// Abstract base class for virtual machine manager.
    /// </summary>
    public class VirtualMachineDeploymentManager : IDeploymentManager
    {
        private const string LogBase = "virtual_machine_manager";

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualMachineDeploymentManager"/> class.
        /// </summary>
        /// <param name="clientFactory">Azure client factory.</param>
        /// <param name="queueProvider">Queue provider object.</param>
        /// <param name="createVirtualMachineStrategies">Create strategies for virtual machines.</param>
        public VirtualMachineDeploymentManager(
            IAzureClientFactory clientFactory,
            IQueueProvider queueProvider,
            IEnumerable<ICreateVirtualMachineStrategy> createVirtualMachineStrategies)
        {
            this.ClientFactory = Requires.NotNull(clientFactory, nameof(clientFactory));
            QueueProvider = Requires.NotNull(queueProvider, nameof(queueProvider));
            CreateVirtualMachineStrategies = createVirtualMachineStrategies;
        }

        /// <summary>
        /// Gets the azure client factory object.
        /// </summary>
        public IAzureClientFactory ClientFactory { get; }

        /// <summary>
        /// Gets queue client provider.
        /// </summary>
        private IQueueProvider QueueProvider { get; }

        private IEnumerable<ICreateVirtualMachineStrategy> CreateVirtualMachineStrategies { get; }

        /// <summary>
        /// Adds relevant tags for the resource, so that it orphaned resource worker knows what to do.
        /// </summary>
        /// <param name="components">Resource components.</param>
        /// <param name="virtualMachineName">Virtual machine name.</param>
        /// <param name="resourceTags">Resource tag dictionary.</param>
        public static void UpdateResourceTags(IList<ResourceComponent> components, string virtualMachineName, IDictionary<string, string> resourceTags)
        {
            resourceTags[ResourceTagName.ResourceName] = virtualMachineName;

            var componentRecordIds = default(string);
            if (components != default)
            {
                componentRecordIds = string.Join(",", components.Where(x => !string.IsNullOrWhiteSpace(x.ResourceRecordId)).Select(x => x.ResourceRecordId));
            }

            if (componentRecordIds != default)
            {
                resourceTags[ResourceTagName.ResourceComponentRecordIds] = componentRecordIds;
            }
        }

        /// <inheritdoc/>
        public Task<(OperationState OperationState, NextStageInput NextInput)> BeginCreateComputeAsync(
            VirtualMachineProviderCreateInput input,
            IDiagnosticsLogger logger)
        {
            try
            {
                var createStrategy = CreateVirtualMachineStrategies.Where(s => s.Accepts(input)).Single();
                return createStrategy.BeginCreateVirtualMachine(input, logger);
            }
            catch (Exception ex)
            {
                logger.LogException($"{LogBase}_begin_create_error", ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<(OperationState OperationState, NextStageInput NextInput)> CheckCreateComputeStatusAsync(
            NextStageInput input,
            IDiagnosticsLogger logger)
        {
            try
            {
                var resource = input.AzureResourceInfo;
                var azure = await ClientFactory.GetAzureClientAsync(resource.SubscriptionId);
                var operationState = await DeploymentUtils.CheckArmResourceDeploymentState(
                    azure,
                    input.TrackingId,
                    resource.ResourceGroup);

                return (operationState,
                     new NextStageInput()
                     {
                         TrackingId = input.TrackingId,
                         AzureResourceInfo = input.AzureResourceInfo,
                     });
            }
            catch (DeploymentException deploymentException)
            {
                logger.LogException($"{LogBase}_check_create_status_error", deploymentException);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogException($"{LogBase}_check_create_status_error", ex);
                if (input.RetryAttempt < 5)
                {
                    return (OperationState.InProgress, new NextStageInput(input.TrackingId, input.AzureResourceInfo, input.RetryAttempt + 1));
                }

                throw;
            }
        }

        /// <inheritdoc/>
        public Task<(OperationState OperationState, NextStageInput NextInput)> BeginDeleteComputeAsync(
             VirtualMachineProviderDeleteInput input,
             IDiagnosticsLogger logger)
        {
            try
            {
                var vmName = input.AzureResourceInfo.Name;
                var vmResourceGroup = input.AzureResourceInfo.ResourceGroup;
                var vmSubscriptionId = input.AzureResourceInfo.SubscriptionId;

                // Save resource state for continuation token.
                var phase0Resources = new Dictionary<string, (AzureResourceInfo ResourceInfo, OperationState ResourceState)>();
                var phase1Resources = new Dictionary<string, (AzureResourceInfo ResourceInfo, OperationState ResourceState)>();
                var phase2Resources = new Dictionary<string, (AzureResourceInfo ResourceInfo, OperationState ResourceState)>();
                var resourceDeletionPlan = new Dictionary<int, (Dictionary<string, (AzureResourceInfo ResourceInfo, OperationState ResourceState)> Resources, OperationState PhaseState)>();
                phase0Resources.Add(VirtualMachineConstants.VmNameKey, (input.AzureResourceInfo, OperationState.NotStarted));
                phase0Resources.Add(VirtualMachineConstants.InputQueueNameKey, (new AzureResourceInfo() { Name = VirtualMachineResourceNames.GetInputQueueName(vmName), ResourceGroup = vmResourceGroup, SubscriptionId = vmSubscriptionId }, OperationState.NotStarted));

                // Add NIC and Disk
                var nicResourceInfo = input.CustomComponents?.Where(c => c != default && c.ComponentType == ResourceType.NetworkInterface).SingleOrDefault()?.AzureResourceInfo;
                if (nicResourceInfo != null)
                {
                    phase1Resources.Add(VirtualMachineConstants.NicNameKey, (nicResourceInfo, OperationState.NotStarted));
                }
                else
                {
                    phase1Resources.Add(VirtualMachineConstants.NicNameKey, (new AzureResourceInfo() { Name = VirtualMachineResourceNames.GetNetworkInterfaceName(vmName), ResourceGroup = vmResourceGroup, SubscriptionId = vmSubscriptionId }, OperationState.NotStarted));
                    phase2Resources.Add(VirtualMachineConstants.NsgNameKey, (new AzureResourceInfo() { Name = VirtualMachineResourceNames.GetNetworkSecurityGroupName(vmName), ResourceGroup = vmResourceGroup, SubscriptionId = vmSubscriptionId }, OperationState.NotStarted));
                    phase2Resources.Add(VirtualMachineConstants.VnetNameKey, (new AzureResourceInfo() { Name = VirtualMachineResourceNames.GetVirtualNetworkName(vmName), ResourceGroup = vmResourceGroup, SubscriptionId = vmSubscriptionId }, OperationState.NotStarted));
                }

                var diskResourceInfo = input.CustomComponents?.Where(c => c != default && c.ComponentType == ResourceType.OSDisk).SingleOrDefault();

                if (diskResourceInfo != default && !diskResourceInfo.Preserve)
                {
                    phase1Resources.Add(VirtualMachineConstants.DiskNameKey, (diskResourceInfo.AzureResourceInfo, OperationState.NotStarted));
                }
                else if (diskResourceInfo == default)
                {
                    phase1Resources.Add(VirtualMachineConstants.DiskNameKey, (new AzureResourceInfo() { Name = VirtualMachineResourceNames.GetOsDiskName(vmName), ResourceGroup = vmResourceGroup, SubscriptionId = vmSubscriptionId }, OperationState.NotStarted));
                }

                resourceDeletionPlan.Add(0, (phase0Resources, OperationState.NotStarted));
                resourceDeletionPlan.Add(1, (phase1Resources, OperationState.NotStarted));
                resourceDeletionPlan.Add(2, (phase2Resources, (phase0Resources.Count == 0) ? OperationState.Succeeded : OperationState.NotStarted));

                var trackingId = JsonConvert.SerializeObject((input.AzureVmLocation, resourceDeletionPlan));
                return Task.FromResult((OperationState.InProgress, new NextStageInput()
                {
                    TrackingId = trackingId,
                    AzureResourceInfo = input.AzureResourceInfo,
                    Version = 1,
                }));
            }
            catch (Exception ex)
            {
                logger.LogException($"{LogBase}_begin_delete_compute_error", ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<(OperationState OperationState, NextStageInput NextInput)> CheckDeleteComputeStatusAsync(
            NextStageInput input,
            IDiagnosticsLogger logger)
        {
            try
            {
                if (input.Version == 0)
                {
                    return await CheckDeleteComputeStatusVer0Async(input, logger);
                }
                else if (input.Version == 1)
                {
                    return await CheckDeleteComputeStatusVer1Async(input, logger);
                }
                else
                {
                    throw new ArgumentException($"{nameof(input)} version {input.Version} is not valid.");
                }
            }
            catch (AggregateException ex)
            {
                var s = new StringBuilder();
                foreach (var e in ex.Flatten().InnerExceptions)
                {
                    s.AppendLine("Exception type: " + e.GetType().FullName);
                    s.AppendLine("Message       : " + e.Message);
                    s.AppendLine("Stacktrace:");
                    s.AppendLine(e.StackTrace);
                    s.AppendLine();
                }

                logger.LogErrorWithDetail($"{LogBase}__error", s.ToString());
                NextStageInput nextStageInput = new NextStageInput(input.TrackingId, input.AzureResourceInfo, input.RetryAttempt + 1);
                if (input.RetryAttempt < 5)
                {
                    return (OperationState.InProgress, nextStageInput);
                }

                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<(OperationState OperationState, int RetryAttempt)> ShutdownComputeAsync(VirtualMachineProviderShutdownInput input, int retryAttempt, IDiagnosticsLogger logger)
        {
            try
            {
                var vmAgentQueueMessage = new QueueMessage
                {
                    Command = "ShutdownEnvironment",
                    Id = input.EnvironmentId,
                };

                await QueueProvider.PushMessageAsync(
                    input.AzureVmLocation,
                    VirtualMachineResourceNames.GetInputQueueName(input.AzureResourceInfo.Name),
                    vmAgentQueueMessage,
                    logger);

                return (OperationState.Succeeded, 0);
            }
            catch (Exception ex)
            {
                logger.LogException($"{LogBase}_shutdown_compute_error", ex);

                if (retryAttempt < 5)
                {
                    return (OperationState.InProgress, retryAttempt + 1);
                }

                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<(OperationState OperationState, int RetryAttempt)> StartComputeAsync(VirtualMachineProviderStartComputeInput input, int retryAttemptCount, IDiagnosticsLogger logger)
        {
            try
            {
                // create queue message
                var jobParameters = new Dictionary<string, string>();
                foreach (var kvp in input.VmInputParams)
                {
                    jobParameters.Add(kvp.Key, kvp.Value);
                }

                if (input.FileShareConnection != null)
                {
                    jobParameters.Add("storageAccountName", input.FileShareConnection.StorageAccountName);
                    jobParameters.Add("storageAccountKey", input.FileShareConnection.StorageAccountKey);
                    jobParameters.Add("storageShareName", input.FileShareConnection.StorageShareName);
                    jobParameters.Add("storageFileName", input.FileShareConnection.StorageFileName);
                }

                // Temporary: Add sku so the vm agent can limit memory on DS4_v3 VMs.
                jobParameters.Add("skuName", input.SkuName);

                var queueMessage = new QueueMessage
                {
                    Command = "StartEnvironment",
                    Parameters = jobParameters,
                };

                await QueueProvider.PushMessageAsync(
                    input.Location,
                    VirtualMachineResourceNames.GetInputQueueName(input.AzureResourceInfo.Name),
                    queueMessage,
                    logger);

                return (OperationState.Succeeded, 0);
            }
            catch (Exception ex)
            {
                logger.LogException($"{LogBase}_start_compute_error", ex);

                if (retryAttemptCount < 5)
                {
                    return (OperationState.InProgress, retryAttemptCount + 1);
                }

                throw;
            }
        }

        private static string CreateVmDeletionTrackingId(AzureLocation computeVmLocation, Dictionary<string, VmResourceState> resourcesToBeDeleted)
        {
            return JsonConvert.SerializeObject((computeVmLocation, resourcesToBeDeleted));
        }

        private static OperationState GetFinalState(Dictionary<string, VmResourceState> resourcesToBeDeleted)
        {
            if (resourcesToBeDeleted.Any(r => r.Value.State == OperationState.InProgress || r.Value.State == OperationState.NotStarted))
            {
                return OperationState.InProgress;
            }

            return OperationState.Succeeded;
        }

        /// <summary>
        /// Checks the status of the resource.
        /// </summary>
        /// <typeparam name="TResult">type param.</typeparam>
        /// <param name="deleteResourceFunc">delete resource delegate.</param>
        /// <param name="checkResourceFunc">check resource delegate.</param>
        /// <param name="resourcesToBeDeleted">list of resources to be deleted.</param>
        /// <param name="resourceNameKey">resource name key.</param>
        /// <returns>A task.</returns>
        private static Task CheckResourceStatus<TResult>(
           Func<string, Task> deleteResourceFunc,
           Func<string, Task<TResult>> checkResourceFunc,
           Dictionary<string, VmResourceState> resourcesToBeDeleted,
           string resourceNameKey)
        {
            var resourceName = resourcesToBeDeleted[resourceNameKey].Name;
            var resourceState = resourcesToBeDeleted[resourceNameKey].State;
            if (resourceState == OperationState.NotStarted)
            {
                var beginDeleteTask = deleteResourceFunc(resourceName);
                return beginDeleteTask.ContinueWith(
                        (task) =>
                        {
                            if (task.IsCompleted)
                            {
                                resourcesToBeDeleted[resourceNameKey] = (resourceName, OperationState.InProgress);
                            }
                            else if (task.IsFaulted || task.IsCanceled)
                            {
                                resourcesToBeDeleted[resourceNameKey] = (resourceName, OperationState.NotStarted);
                            }
                        });
            }
            else if (resourceState == OperationState.InProgress)
            {
                var checkStatusTask = checkResourceFunc(resourceName);
                return checkStatusTask.ContinueWith(
                        (task) =>
                        {
                            if (task.IsCompleted && task.Result == null)
                            {
                                resourcesToBeDeleted[resourceNameKey] = (resourceName, OperationState.Succeeded);
                            }
                        });
            }

            return Task.CompletedTask;
        }

        private async Task<(OperationState OperationState, NextStageInput NextInput)> CheckDeleteComputeStatusVer1Async(NextStageInput input, IDiagnosticsLogger logger)
        {
            var (computeVmLocation, resourceDeletionPlan) = JsonConvert
                            .DeserializeObject<(AzureLocation, Dictionary<int, (Dictionary<string, (AzureResourceInfo ResourceInfo, OperationState ResourceState)> Resources, OperationState PhaseState)>)>(input.TrackingId);

            var resourceToBeDeleted = (Dictionary<string, (AzureResourceInfo ResourceInfo, OperationState ResourceState)>)default;
            var phase = 0;
            for (int i = 0; i < resourceDeletionPlan.Count; i++)
            {
                if (resourceDeletionPlan.ContainsKey(i) && resourceDeletionPlan[i].PhaseState != OperationState.Succeeded)
                {
                    resourceToBeDeleted = resourceDeletionPlan[i].Resources;
                    phase = i;
                    break;
                }
            }

            if (resourceToBeDeleted == default || resourceToBeDeleted.Count == 0)
            {
                return (OperationState.Succeeded, input);
            }

            var taskList = new List<Task>();
            var resourceDeletionStatus = new Dictionary<string, VmResourceState>();
            foreach (var resource in resourceToBeDeleted)
            {
                resourceDeletionStatus[resource.Key] = (resource.Value.ResourceInfo.Name, resourceToBeDeleted[resource.Key].ResourceState);
                var resourceGroup = resource.Value.ResourceInfo.ResourceGroup;
                var subscriptionId = resource.Value.ResourceInfo.SubscriptionId;
                var azureClient = await ClientFactory.GetAzureClientAsync(subscriptionId);
                switch (resource.Key)
                {
                    case VirtualMachineConstants.VmNameKey:
                        var vmComputeClient = await ClientFactory.GetComputeManagementClient(subscriptionId);
                        taskList.Add(
                          CheckResourceStatus(
                             (resourceName) => vmComputeClient.VirtualMachines.BeginDeleteAsync(resourceGroup, resourceName),
                             (resourceName) => azureClient.VirtualMachines.GetByResourceGroupAsync(resourceGroup, resourceName),
                             resourceDeletionStatus,
                             resource.Key));
                        break;
                    case VirtualMachineConstants.InputQueueNameKey:
                        taskList.Add(
                            CheckResourceStatus(
                               (resourceName) => QueueProvider.DeleteQueueAsync(computeVmLocation, resourceName, logger),
                               (resourceName) => QueueProvider.QueueExistsAync(computeVmLocation, resourceName, logger),
                               resourceDeletionStatus,
                               resource.Key));
                        break;
                    case VirtualMachineConstants.DiskNameKey:
                        var diskComputeClient = await ClientFactory.GetComputeManagementClient(subscriptionId);
                        taskList.Add(CheckResourceStatus(
                            (resourceName) => diskComputeClient.Disks.BeginDeleteAsync(resourceGroup, resourceName),
                            (resourceName) => azureClient.Disks.GetByResourceGroupAsync(resourceGroup, resourceName),
                            resourceDeletionStatus,
                            resource.Key));
                        break;
                    case VirtualMachineConstants.NicNameKey:
                        var nicClient = await ClientFactory.GetNetworkManagementClient(subscriptionId);
                        taskList.Add(
                            CheckResourceStatus(
                              (resourceName) => nicClient.NetworkInterfaces.BeginDeleteAsync(resourceGroup, resourceName),
                              (resourceName) => azureClient.NetworkInterfaces.GetByResourceGroupAsync(resourceGroup, resourceName),
                              resourceDeletionStatus,
                              resource.Key));
                        break;
                    case VirtualMachineConstants.NsgNameKey:
                        var nsgClient = await ClientFactory.GetNetworkManagementClient(subscriptionId);
                        taskList.Add(
                           CheckResourceStatus(
                           (resourceName) => nsgClient.NetworkSecurityGroups.BeginDeleteAsync(resourceGroup, resourceName),
                           (resourceName) => azureClient.NetworkSecurityGroups.GetByResourceGroupAsync(resourceGroup, resourceName),
                           resourceDeletionStatus,
                           resource.Key));
                        break;
                    case VirtualMachineConstants.VnetNameKey:
                        var vnetClient = await ClientFactory.GetNetworkManagementClient(subscriptionId);
                        taskList.Add(
                            CheckResourceStatus(
                            (resourceName) => vnetClient.VirtualNetworks.BeginDeleteAsync(resourceGroup, resourceName),
                            (resourceName) => azureClient.Networks.GetByResourceGroupAsync(resourceGroup, resourceName),
                            resourceDeletionStatus,
                            resource.Key));
                        break;
                }
            }

            await Task.WhenAll(taskList);
            foreach (var resource in resourceDeletionStatus)
            {
                if (resource.Value.State != resourceToBeDeleted[resource.Key].ResourceState)
                {
                    resourceToBeDeleted[resource.Key] = (resourceToBeDeleted[resource.Key].ResourceInfo, resource.Value.State);
                }
            }

            var resultState = GetFinalState(resourceDeletionStatus);
            resourceDeletionPlan[phase] = (resourceToBeDeleted, resultState);
            var trackingId = JsonConvert.SerializeObject((computeVmLocation, resourceDeletionPlan));
            return (OperationState.InProgress, new NextStageInput()
            {
                TrackingId = trackingId,
                AzureResourceInfo = input.AzureResourceInfo,
                Version = 1,
            });
        }

        private async Task<(OperationState OperationState, NextStageInput NextInput)> CheckDeleteComputeStatusVer0Async(NextStageInput input, IDiagnosticsLogger logger)
        {
            var (computeVmLocation, resourcesToBeDeleted) = JsonConvert
            .DeserializeObject<(AzureLocation, Dictionary<string, VmResourceState>)>(input.TrackingId);
            var resourceGroup = input.AzureResourceInfo.ResourceGroup;
            var azure = await ClientFactory.GetAzureClientAsync(input.AzureResourceInfo.SubscriptionId);
            var networkClient = await ClientFactory.GetNetworkManagementClient(input.AzureResourceInfo.SubscriptionId);
            var computeClient = await ClientFactory.GetComputeManagementClient(input.AzureResourceInfo.SubscriptionId);

            if (resourcesToBeDeleted[VirtualMachineConstants.VmNameKey].State != OperationState.Succeeded)
            {
                // Check if virtual machine deletion is complete
                var linuxVM = await azure.VirtualMachines
                          .GetByResourceGroupAsync(resourceGroup, resourcesToBeDeleted[VirtualMachineConstants.VmNameKey].Name);
                if (linuxVM != null)
                {
                    return (OperationState.InProgress, input);
                }
                else
                {
                    resourcesToBeDeleted[VirtualMachineConstants.VmNameKey] = (resourcesToBeDeleted[VirtualMachineConstants.VmNameKey].Name, OperationState.Succeeded);
                }
            }

            // Virtual machine is deleted, delete the remaining resources
            var taskList = new List<Task>
            {
                CheckResourceStatus(
                (resourceName) => QueueProvider.DeleteQueueAsync(computeVmLocation, resourceName, logger),
                (resourceName) => QueueProvider.QueueExistsAync(computeVmLocation, resourceName, logger),
                resourcesToBeDeleted,
                VirtualMachineConstants.InputQueueNameKey),

                CheckResourceStatus(
                (resourceName) => computeClient.Disks.BeginDeleteAsync(resourceGroup, resourceName),
                (resourceName) => azure.Disks.GetByResourceGroupAsync(resourceGroup, resourceName),
                resourcesToBeDeleted,
                VirtualMachineConstants.DiskNameKey),

                CheckResourceStatus(
                  (resourceName) => networkClient.NetworkInterfaces.BeginDeleteAsync(resourceGroup, resourceName),
                  (resourceName) => azure.NetworkInterfaces.GetByResourceGroupAsync(resourceGroup, resourceName),
                  resourcesToBeDeleted,
                  VirtualMachineConstants.NicNameKey),
            };

            if (resourcesToBeDeleted[VirtualMachineConstants.NicNameKey].State == OperationState.Succeeded)
            {
                taskList.Add(
                    CheckResourceStatus(
                    (resourceName) => networkClient.NetworkSecurityGroups.BeginDeleteAsync(resourceGroup, resourceName),
                    (resourceName) => azure.NetworkSecurityGroups.GetByResourceGroupAsync(resourceGroup, resourceName),
                    resourcesToBeDeleted,
                    VirtualMachineConstants.NsgNameKey));

                taskList.Add(
                    CheckResourceStatus(
                    (resourceName) => networkClient.VirtualNetworks.BeginDeleteAsync(resourceGroup, resourceName),
                    (resourceName) => azure.Networks.GetByResourceGroupAsync(resourceGroup, resourceName),
                    resourcesToBeDeleted,
                    VirtualMachineConstants.VnetNameKey));
            }

            await Task.WhenAll(taskList);
            var nextStageInput = new NextStageInput(CreateVmDeletionTrackingId(computeVmLocation, resourcesToBeDeleted), input.AzureResourceInfo);
            var resultState = GetFinalState(resourcesToBeDeleted);
            return (resultState, nextStageInput);
        }
    }
}
