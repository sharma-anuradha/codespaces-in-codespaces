// <copyright file="VirtualMachineDeploymentManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.Network.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine.Strategies;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.NetworkInterfaceProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Contracts;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine
{
    /// <summary>
    /// Abstract base class for virtual machine manager.
    /// </summary>
    public class VirtualMachineDeploymentManager : IDeploymentManager
    {
        private const string StopInProgressStatusCode = "PowerState/stopping";
        private const string StoppedStatusCode = "PowerState/stopped";
        private const string DeleteInProgressStatusCode = "ProvisioningState/deleting";
        private const string LogBase = "virtual_machine_manager";

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualMachineDeploymentManager"/> class.
        /// </summary>
        /// <param name="clientFactory">Azure client factory.</param>
        /// <param name="clientFPAFactory">Azure client first party app factory.</param>
        /// <param name="queueProvider">Queue provider object.</param>
        /// <param name="createVirtualMachineStrategies">Create strategies for virtual machines.</param>
        public VirtualMachineDeploymentManager(
            IAzureClientFactory clientFactory,
            IAzureClientFPAFactory clientFPAFactory,
            IQueueProvider queueProvider,
            IEnumerable<ICreateVirtualMachineStrategy> createVirtualMachineStrategies)
        {
            ClientFactory = Requires.NotNull(clientFactory, nameof(clientFactory));
            ClientFPAFactory = Requires.NotNull(clientFPAFactory, nameof(clientFPAFactory));
            QueueProvider = Requires.NotNull(queueProvider, nameof(queueProvider));
            CreateVirtualMachineStrategies = createVirtualMachineStrategies;
        }

        /// <summary>
        /// Gets the azure client factory object.
        /// </summary>
        private IAzureClientFactory ClientFactory { get; }

        private IAzureClientFPAFactory ClientFPAFactory { get; }

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
        public async Task<OperationState> ApplyNsgRulesAsync(AzureResourceInfo azureResourceInfo, ResourceComponent niComponent, IDiagnosticsLogger logger)
        {
            Requires.NotNull(logger, nameof(logger));

            return await logger.OperationScopeAsync(
                $"{LogBase}_apply_nsg_rules",
                async (childLogger) =>
                {
                    var azure = niComponent != default ?
                        await ClientFactory.GetAzureClientAsync(niComponent.AzureResourceInfo.SubscriptionId) :
                        await ClientFactory.GetAzureClientAsync(azureResourceInfo.SubscriptionId);

                    INetworkSecurityGroup networkSecurityGroup;
                    if (niComponent == default)
                    {
                        var nsgName = VirtualMachineResourceNames.GetNetworkSecurityGroupName(azureResourceInfo.Name);
                        networkSecurityGroup = await azure.NetworkSecurityGroups.GetByResourceGroupAsync(
                            azureResourceInfo.ResourceGroup,
                            nsgName);
                    }
                    else
                    {
                        var nicProperties = new AzureResourceInfoNetworkInterfaceProperties(niComponent.AzureResourceInfo.Properties);
                        networkSecurityGroup = await azure.NetworkSecurityGroups.GetByResourceGroupAsync(
                            niComponent.AzureResourceInfo.ResourceGroup,
                            nicProperties.Nsg);
                    }

                    var nsgUpdatable = networkSecurityGroup.Update();

                    nsgUpdatable.DefineRule("Restrict-Outbound-AzurePlatformIMDS-Rule")
                        .DenyOutbound()
                        .FromAnyAddress()
                        .FromAnyPort()
                        .ToAddress("AzurePlatformIMDS")
                        .ToAnyPort()
                        .WithAnyProtocol()
                        .WithPriority(300)
                        .Attach();

                    nsgUpdatable.DefineRule("Restrict-Outbound-Udp-Rule")
                        .DenyOutbound()
                        .FromAnyAddress()
                        .FromAnyPort()
                        .ToAnyAddress()
                        .ToAnyPort()
                        .WithProtocol(SecurityRuleProtocol.Udp)
                        .WithPriority(290)
                        .Attach();

                    nsgUpdatable.DefineRule("Allow-Outbound-Udp-Skype-Rule")
                        .AllowOutbound()
                        .FromAnyAddress()
                        .FromAnyPort()
                        .ToAddresses(new[]
                        {
                            "13.107.64.0/18",
                            "52.112.0.0/14",
                            "52.120.0.0/14",
                        })
                        .ToPortRanges(new[]
                        {
                            "3478",
                            "3479",
                            "3480",
                            "3481",
                        })
                        .WithProtocol(SecurityRuleProtocol.Udp)
                        .WithPriority(280)
                        .Attach();

                    var nsgResult = await nsgUpdatable.ApplyAsync();

                    childLogger.FluentAddValue("SecurityRules", string.Join(", ", nsgResult.SecurityRules.Keys));
                    childLogger.FluentAddValue("DefaultSecurityRules", string.Join(", ", nsgResult.DefaultSecurityRules.Keys));

                    return OperationState.Succeeded;
                });
        }

        /// <inheritdoc/>
        public async Task<OperationState> UpdateTagsAsync(
            VirtualMachineProviderUpdateTagsInput input,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(input, nameof(input));
            Requires.NotNull(logger, nameof(logger));

            return await logger.OperationScopeAsync(
                $"{LogBase}_update_tags",
                async (childLogger) =>
                {
                    var azure = (input.CustomComponents != default &&
                    input.CustomComponents.Any(c => c.ComponentType == ResourceType.NetworkInterface)) ?
                        await ClientFPAFactory.GetAzureClientAsync(input.VirtualMachineResourceInfo.SubscriptionId, childLogger) :
                        await ClientFactory.GetAzureClientAsync(input.VirtualMachineResourceInfo.SubscriptionId, childLogger);

                    var virtualMachine = await azure.VirtualMachines.GetByResourceGroupAsync(input.VirtualMachineResourceInfo.ResourceGroup, input.VirtualMachineResourceInfo.Name);
                    var mergedTags = GetMergedTags(virtualMachine.Tags, input.AdditionalComputeResourceTags);
                    await virtualMachine.Update()
                            .WithTags(mergedTags)
                            .ApplyAsync();

                    return OperationState.Succeeded;
                });
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
                var azure = await ClientFactory.GetAzureClientAsync(resource.SubscriptionId, logger.NewChildLogger());
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
        public async Task<(OperationState OperationState, NextStageInput NextInput)> BeginDeleteComputeAsync(
             VirtualMachineProviderDeleteInput input,
             IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
                $"{LogBase}_begin_delete_compute",
                async (childLogger) =>
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

                    // Add Queue
                    var queueResourceInfo = input.CustomComponents?.Where(c => c != default && c.ComponentType == ResourceType.InputQueue).SingleOrDefault();
                    if (queueResourceInfo != default && !queueResourceInfo.Preserve)
                    {
                        phase0Resources.Add(VirtualMachineConstants.InputQueueNameKey, (queueResourceInfo.AzureResourceInfo, OperationState.NotStarted));
                    }
                    else if (queueResourceInfo == default)
                    {
                        var queueDetailsInput = new QueueProviderGetDetailsInput()
                        {
                            AzureLocation = input.AzureVmLocation,
                            Name = VirtualMachineResourceNames.GetInputQueueName(vmName),
                        };

                        var queueDetailsResult = await QueueProvider.GetDetailsAsync(queueDetailsInput, childLogger.NewChildLogger());
                        phase0Resources.Add(VirtualMachineConstants.InputQueueNameKey, (queueDetailsResult.AzureResourceInfo, OperationState.NotStarted));
                    }

                    // Add NIC
                    var nicResourceInfo = input.CustomComponents?.Where(c => c != default && c.ComponentType == ResourceType.NetworkInterface).SingleOrDefault()?.AzureResourceInfo;
                    if (nicResourceInfo != null)
                    {
                        var nicInfoProperties = new AzureResourceInfoNetworkInterfaceProperties(nicResourceInfo.Properties);

                        phase1Resources.Add(VirtualMachineConstants.NicNameKey, (nicResourceInfo, OperationState.NotStarted));

                        if (!nicInfoProperties.IsVNetInjected)
                        {
                            phase2Resources.Add(VirtualMachineConstants.NsgNameKey, (new AzureResourceInfo() { Name = nicInfoProperties.Nsg, ResourceGroup = nicResourceInfo.ResourceGroup, SubscriptionId = nicResourceInfo.SubscriptionId }, OperationState.NotStarted));
                            phase2Resources.Add(VirtualMachineConstants.VnetNameKey, (new AzureResourceInfo() { Name = nicInfoProperties.VNet, ResourceGroup = nicResourceInfo.ResourceGroup, SubscriptionId = nicResourceInfo.SubscriptionId }, OperationState.NotStarted));
                        }
                    }
                    else
                    {
                        phase1Resources.Add(VirtualMachineConstants.NicNameKey, (new AzureResourceInfo() { Name = VirtualMachineResourceNames.GetNetworkInterfaceName(vmName), ResourceGroup = vmResourceGroup, SubscriptionId = vmSubscriptionId }, OperationState.NotStarted));
                        phase2Resources.Add(VirtualMachineConstants.NsgNameKey, (new AzureResourceInfo() { Name = VirtualMachineResourceNames.GetNetworkSecurityGroupName(vmName), ResourceGroup = vmResourceGroup, SubscriptionId = vmSubscriptionId }, OperationState.NotStarted));
                        phase2Resources.Add(VirtualMachineConstants.VnetNameKey, (new AzureResourceInfo() { Name = VirtualMachineResourceNames.GetVirtualNetworkName(vmName), ResourceGroup = vmResourceGroup, SubscriptionId = vmSubscriptionId }, OperationState.NotStarted));
                    }

                    // Add Disk
                    var diskResourceInfo = input.CustomComponents?.Where(c => c != default && c.ComponentType == ResourceType.OSDisk).SingleOrDefault();
                    if (diskResourceInfo != default && !diskResourceInfo.Preserve)
                    {
                        phase1Resources.Add(VirtualMachineConstants.DiskNameKey, (diskResourceInfo.AzureResourceInfo, OperationState.NotStarted));
                    }
                    else if (diskResourceInfo == default)
                    {
                        var ephemeralOSDiskProperties = new AzureResourceInfoEphemeralOSDiskProperties(input.AzureResourceInfo.Properties);
                        if (!ephemeralOSDiskProperties.UsesEphemeralOSDisk)
                        {
                            phase1Resources.Add(VirtualMachineConstants.DiskNameKey, (new AzureResourceInfo() { Name = VirtualMachineResourceNames.GetOsDiskName(vmName), ResourceGroup = vmResourceGroup, SubscriptionId = vmSubscriptionId }, OperationState.NotStarted));
                        }
                    }

                    resourceDeletionPlan.Add(0, (phase0Resources, OperationState.NotStarted));
                    resourceDeletionPlan.Add(1, (phase1Resources, OperationState.NotStarted));
                    resourceDeletionPlan.Add(2, (phase2Resources, (phase0Resources.Count == 0) ? OperationState.Succeeded : OperationState.NotStarted));

                    var trackingId = JsonConvert.SerializeObject((input.AzureVmLocation, resourceDeletionPlan));

                    childLogger.FluentAddValue("TrackingId", trackingId);

                    return (OperationState.InProgress, new NextStageInput()
                    {
                        TrackingId = trackingId,
                        AzureResourceInfo = input.AzureResourceInfo,
                        Version = NextStageInput.CurrentVersion,
                    });
                },
                swallowException: false);
        }

        /// <inheritdoc/>
        public async Task<(OperationState OperationState, NextStageInput NextInput)> CheckDeleteComputeStatusAsync(
            NextStageInput input,
            IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
                $"{LogBase}_check_delete_compute",
                async (childLogger) =>
                {
                    if (input.Version == NextStageInput.CurrentVersion)
                    {
                        return await CheckDeleteComputeStatusVer1Async(input, childLogger.NewChildLogger());
                    }
                    else
                    {
                        throw new ArgumentException($"{nameof(input)} version {input.Version} is not valid.");
                    }
                },
                (ex, childLogger) =>
                {
                    if (ex is ArgumentException)
                    {
                        throw ex;
                    }
                    else if (ex is AggregateException aggEx)
                    {
                        var s = new StringBuilder();
                        foreach (var e in aggEx.Flatten().InnerExceptions)
                        {
                            s.AppendLine("Exception type: " + e.GetType().FullName);
                            s.AppendLine("Message       : " + e.Message);
                            s.AppendLine("Stacktrace:");
                            s.AppendLine(e.StackTrace);
                            s.AppendLine();
                        }

                        logger
                            .FluentAddValue("RetryAttempt", input.RetryAttempt)
                            .LogErrorWithDetail($"{LogBase}_check_delete_compute_error", s.ToString());

                        var nextStageInput = new NextStageInput(input.TrackingId, input.AzureResourceInfo, input.RetryAttempt + 1)
                        {
                            Version = NextStageInput.CurrentVersion,
                        };
                        if (input.RetryAttempt < 5)
                        {
                            return Task.FromResult((OperationState.InProgress, nextStageInput));
                        }

                        throw aggEx;
                    }
                    else
                    {
                        throw ex;
                    }
                },
                swallowException: false);
        }

        /// <inheritdoc/>
        public async Task<(OperationState OperationState, int RetryAttempt)> ShutdownComputeAsync(VirtualMachineProviderShutdownInput input, int retryAttempt, IDiagnosticsLogger logger)
        {
            try
            {
                await QueueProvider.PushMessageAsync(
                    input.AzureVmLocation,
                    VirtualMachineResourceNames.GetInputQueueName(input.AzureResourceInfo.Name),
                    input.GenerateShutdownEnvironmentPayload(),
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
        public async Task<(OperationState OperationState, int RetryAttempt)> StartComputeAsync(
            VirtualMachineProviderStartComputeInput input,
            int retryAttemptCount,
            IDiagnosticsLogger logger)
        {
            try
            {
                // create queue message
                var queueMessage = input.GenerateStartEnvironmentPayload();

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
        /// <param name="logger">a diagnostics logger to which new columns are added - it should NOT be a new logger.</param>
        /// <returns>A task.</returns>
        private static Task CheckResourceStatus<TResult>(
           Func<string, Task> deleteResourceFunc,
           Func<string, Task<TResult>> checkResourceFunc,
           Dictionary<string, VmResourceState> resourcesToBeDeleted,
           string resourceNameKey,
           IDiagnosticsLogger logger)
        {
            var resourceName = resourcesToBeDeleted[resourceNameKey].Name;
            var resourceState = resourcesToBeDeleted[resourceNameKey].State;

            logger.FluentAddValue("ResourceOriginalOperationState", resourceState.ToString());

            if (resourceState == OperationState.NotStarted)
            {
                var beginDeleteTask = deleteResourceFunc(resourceName);
                return beginDeleteTask.ContinueWith(
                        (task) =>
                        {
                            if (task.IsCompleted)
                            {
                                logger.FluentAddValue("ResourceNewOperationState", OperationState.InProgress.ToString());
                                resourcesToBeDeleted[resourceNameKey] = (resourceName, OperationState.InProgress);
                            }
                            else if (task.IsFaulted || task.IsCanceled)
                            {
                                logger.FluentAddValue("ResourceNewOperationState", OperationState.NotStarted.ToString());
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
                                logger.FluentAddValue("ResourceNewOperationState", OperationState.Succeeded.ToString());
                                resourcesToBeDeleted[resourceNameKey] = (resourceName, OperationState.Succeeded);
                            }
                        });
            }

            return Task.CompletedTask;
        }

        private static IDictionary<string, string> GetMergedTags(IReadOnlyDictionary<string, string> existingTags, IDictionary<string, string> newTags)
        {
            var mergedTags = new Dictionary<string, string>();

            if (existingTags != default)
            {
                foreach (var tag in existingTags)
                {
                    mergedTags.Add(tag.Key, tag.Value);
                }
            }

            if (newTags != default)
            {
                foreach (var tag in newTags)
                {
                    // Overwrites.
                    mergedTags[tag.Key] = tag.Value;
                }
            }

            return mergedTags;
        }

        /// <summary>
        /// Gets the Virtual machine's statuses.
        /// </summary>
        /// <param name="resourceGroup"> Resource group name.</param>
        /// <param name="vmName"> Virtual machine name.</param>
        /// <param name="vmComputeClient"> Vm compute client for accessing api.</param>
        /// <param name="logger"> Logger to be used.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task<IEnumerable<InstanceViewStatus>> CheckVirtualMachineStatusAsync(string resourceGroup, string vmName, IComputeManagementClient vmComputeClient, IDiagnosticsLogger logger)
        {
            try
            {
                var instanceView = await vmComputeClient.VirtualMachines.InstanceViewAsync(resourceGroup, vmName);
                return instanceView.Statuses;
            }
            catch (Exception ex)
            {
                // Virtual machine not found.
                if (ex.Message.Contains("not found", StringComparison.InvariantCultureIgnoreCase))
                {
                    logger.LogException($"Virtual machine {vmName} not found", ex);
                    return new List<InstanceViewStatus>();
                }
                else
                {
                    // Azure/Network exception occurred.
                    logger.LogException($"Azure/Network exception occurred while checking status for Virtual machine {vmName}", ex);
                    return null;
                }
            }
        }

        private async Task<(OperationState OperationState, NextStageInput NextInput)> CheckDeleteComputeStatusVer1Async(NextStageInput input, IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
                $"{LogBase}_check_delete_compute_v1",
                async (childLogger) =>
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

                    childLogger.FluentAddBaseValue("Phase", phase);

                    if (resourceToBeDeleted == default || resourceToBeDeleted.Count == 0)
                    {
                        return (OperationState.Succeeded, input);
                    }

                    var resourceDeletionStatus = new Dictionary<string, VmResourceState>();
                    await Task.WhenAll(resourceToBeDeleted.Select(async (resource) =>
                    {
                        await childLogger.OperationScopeAsync(
                            $"{LogBase}_check_delete_compute_v1_resource",
                            async (resourceLogger) =>
                            {
                                resourceDeletionStatus[resource.Key] = (resource.Value.ResourceInfo.Name, resourceToBeDeleted[resource.Key].ResourceState);
                                var resourceName = resource.Value.ResourceInfo.Name;
                                var resourceGroup = resource.Value.ResourceInfo.ResourceGroup;
                                var subscriptionId = resource.Value.ResourceInfo.SubscriptionId;
                                var nicProperties = new AzureResourceInfoNetworkInterfaceProperties(resource.Value.ResourceInfo.Properties);
                                var isVnetInjected = nicProperties.IsVNetInjected;

                                resourceLogger
                                    .FluentAddBaseValue("SubscriptionId", subscriptionId)
                                    .FluentAddBaseValue("ResourceGroup", resourceGroup)
                                    .FluentAddBaseValue("ResourceName", resourceName)
                                    .FluentAddBaseValue("ResourceStepKey", resource.Key);

                                var azureClient = resource.Key == VirtualMachineConstants.InputQueueNameKey ?
                                    default :
                                    isVnetInjected ?
                                    await ClientFPAFactory.GetAzureClientAsync(subscriptionId, resourceLogger.NewChildLogger()) :
                                    await ClientFactory.GetAzureClientAsync(subscriptionId, resourceLogger.NewChildLogger());

                                switch (resource.Key)
                                {
                                    case VirtualMachineConstants.VmNameKey:
                                        var vmComputeClient = await ClientFactory.GetComputeManagementClient(subscriptionId);
                                        var vmStatuses = await CheckVirtualMachineStatusAsync(resourceGroup, resourceName, vmComputeClient, logger);

                                        // Network/Azure exceptions could have happened, we are supposed to get some status
                                        if (vmStatuses == null)
                                        {
                                            break;
                                        }

                                        // Virtual machine has already been deleted. Hence marking it as succeeded.
                                        if (!vmStatuses.Any())
                                        {
                                            resourceDeletionStatus[VirtualMachineConstants.VmNameKey] = (resourceName, OperationState.Succeeded);
                                            break;
                                        }

                                        var isStopped = vmStatuses.Any(x => string.Equals(x.Code, StoppedStatusCode, StringComparison.OrdinalIgnoreCase));

                                        // Delete the virtual machine once its stopped to prevent file corruption.
                                        if (isStopped)
                                        {
                                            await CheckResourceStatus(
                                                (resourceName) => vmComputeClient.VirtualMachines.BeginDeleteAsync(resourceGroup, resourceName),
                                                (resourceName) => azureClient.VirtualMachines.GetByResourceGroupAsync(resourceGroup, resourceName),
                                                resourceDeletionStatus,
                                                resource.Key,
                                                resourceLogger);
                                            break;
                                        }

                                        var isStopInProgress = vmStatuses.Any(x => string.Equals(x.Code, StopInProgressStatusCode, StringComparison.OrdinalIgnoreCase));
                                        var isDeleteInProgress = vmStatuses.Any(x => string.Equals(x.Code, DeleteInProgressStatusCode, StringComparison.OrdinalIgnoreCase));

                                        // If virtual machine is not stopped, we are going to stop it.
                                        if (!isDeleteInProgress && !isStopInProgress)
                                        {
                                            try
                                            {
                                                await vmComputeClient.VirtualMachines.BeginPowerOffAsync(resourceGroup, resourceName);
                                            }
                                            catch (Exception ex)
                                            {
                                                logger.LogException($"Exception occurred while stopping virtual machine {resourceName}", ex);
                                            }
                                        }

                                        break;
                                    case VirtualMachineConstants.InputQueueNameKey:
                                        var queueDeleteInput = new QueueProviderDeleteInput()
                                        {
                                            AzureResourceInfo = resource.Value.ResourceInfo,
                                        };

                                        await CheckResourceStatus(
                                            (resourceName) => QueueProvider.DeleteAsync(queueDeleteInput, logger),
                                            (resourceName) => QueueProvider.ExistsAync(resource.Value.ResourceInfo, logger),
                                            resourceDeletionStatus,
                                            resource.Key,
                                            resourceLogger);
                                        break;
                                    case VirtualMachineConstants.DiskNameKey:
                                        var diskComputeClient = await ClientFactory.GetComputeManagementClient(subscriptionId);
                                        await CheckResourceStatus(
                                            (resourceName) => diskComputeClient.Disks.BeginDeleteAsync(resourceGroup, resourceName),
                                            (resourceName) => azureClient.Disks.GetByResourceGroupAsync(resourceGroup, resourceName),
                                            resourceDeletionStatus,
                                            resource.Key,
                                            resourceLogger);
                                        break;
                                    case VirtualMachineConstants.NicNameKey:

                                        var nicClient = isVnetInjected ?
                                            await ClientFPAFactory.GetNetworkManagementClient(subscriptionId, resourceLogger.NewChildLogger()) :
                                            await ClientFactory.GetNetworkManagementClient(subscriptionId, resourceLogger.NewChildLogger());
                                        await CheckResourceStatus(
                                            (resourceName) => nicClient.NetworkInterfaces.BeginDeleteAsync(resourceGroup, resourceName),
                                            (resourceName) => azureClient.NetworkInterfaces.GetByResourceGroupAsync(resourceGroup, resourceName),
                                            resourceDeletionStatus,
                                            resource.Key,
                                            resourceLogger);
                                        break;
                                    case VirtualMachineConstants.NsgNameKey:
                                        var nsgClient = await ClientFactory.GetNetworkManagementClient(subscriptionId, resourceLogger.NewChildLogger());
                                        await CheckResourceStatus(
                                            (resourceName) => nsgClient.NetworkSecurityGroups.BeginDeleteAsync(resourceGroup, resourceName),
                                            (resourceName) => azureClient.NetworkSecurityGroups.GetByResourceGroupAsync(resourceGroup, resourceName),
                                            resourceDeletionStatus,
                                            resource.Key,
                                            resourceLogger);
                                        break;
                                    case VirtualMachineConstants.VnetNameKey:
                                        var vnetClient = await ClientFactory.GetNetworkManagementClient(subscriptionId, resourceLogger.NewChildLogger());
                                        await CheckResourceStatus(
                                            (resourceName) => vnetClient.VirtualNetworks.BeginDeleteAsync(resourceGroup, resourceName),
                                            (resourceName) => azureClient.Networks.GetByResourceGroupAsync(resourceGroup, resourceName),
                                            resourceDeletionStatus,
                                            resource.Key,
                                            resourceLogger);
                                        break;
                                }
                            });
                    }));

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

                    childLogger
                        .FluentAddValue("ResultState", resultState.ToString())
                        .FluentAddValue("TrackingId", trackingId);

                    return (OperationState.InProgress, new NextStageInput()
                    {
                        TrackingId = trackingId,
                        AzureResourceInfo = input.AzureResourceInfo,
                        Version = NextStageInput.CurrentVersion,
                    });
                },
                swallowException: false);
        }
    }
}
