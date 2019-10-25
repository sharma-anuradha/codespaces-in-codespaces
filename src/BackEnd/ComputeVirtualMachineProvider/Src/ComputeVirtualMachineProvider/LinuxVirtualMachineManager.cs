// <copyright file="LinuxVirtualMachineManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Queue;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine
{
    /// <summary>
    /// Create, update and deletes Azure virtual machines.
    /// </summary>
    public class LinuxVirtualMachineManager : IDeploymentManager
    {
        private const string Key = "value";
        private const string ExtensionName = "update-vm";
        private const string NicNameKey = "nicName";
        private const string NsgNameKey = "nsgName";
        private const string VnetNameKey = "vnetName";
        private const string DiskNameKey = "diskId";
        private const string InputQueueNameKey = "inputQueueName";
        private const string OutputQueueNameKey = "outputQueueName";
        private const string VmNameKey = "vmName";
        private static readonly string VmTemplateJson = GetVmTemplate();
        private readonly IAzureClientFactory clientFactory;
        private readonly IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor;
        private bool useOutputQueue;

        /// <summary>
        /// Initializes a new instance of the <see cref="LinuxVirtualMachineManager"/> class.
        /// </summary>
        /// <param name="clientFactory">Builds Azure clients.</param>
        /// <param name="vmTokenProvider">VM Token provider.</param>
        /// <param name="controlPlaneAzureResourceAccessor">Control plane azure resource accessor.</param>
        public LinuxVirtualMachineManager(
            IAzureClientFactory clientFactory,
            IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor)
        {
            Requires.NotNull(clientFactory, nameof(clientFactory));
            Requires.NotNull(controlPlaneAzureResourceAccessor, nameof(controlPlaneAzureResourceAccessor));

            this.clientFactory = clientFactory;
            this.controlPlaneAzureResourceAccessor = controlPlaneAzureResourceAccessor;
            SetUseOutputQueueFlag();
        }

        [Conditional("DEBUG")]
        private void SetUseOutputQueueFlag()
        {
            this.useOutputQueue = true;
        }

        /// <inheritdoc/>
        public bool Accepts(ComputeOS computeOS)
        {
            return computeOS == ComputeOS.Linux;
        }

        /// <inheritdoc/>
        public async Task<(OperationState, NextStageInput)> BeginCreateComputeAsync(VirtualMachineProviderCreateInput input, IDiagnosticsLogger logger)
        {
            Requires.NotNull(input.AzureResourceGroup, nameof(input.AzureResourceGroup));
            Requires.NotNull(input.AzureSkuName, nameof(input.AzureSkuName));
            Requires.NotNull(input.AzureVirtualMachineImage, nameof(input.AzureVirtualMachineImage));
            Requires.NotNull(input.VmAgentBlobUrl, nameof(input.VmAgentBlobUrl));
            Requires.NotNull(input.VMToken, nameof(input.VMToken));
            Requires.NotNull(input.ResourceId, nameof(input.ResourceId));
            Requires.NotNull(input.FrontDnsHostName, nameof(input.FrontDnsHostName));

            // create new VM resource name
            var virtualMachineName = Guid.NewGuid().ToString();

            var resourceTags = input.ResourceTags;

            resourceTags.Add(ResourceTagName.ResourceName, virtualMachineName);

            var deploymentName = $"Create-LinuxVm-{virtualMachineName}";

            try
            {
                var azure = await clientFactory.GetAzureClientAsync(input.AzureSubscription);
                await azure.CreateResourceGroupIfNotExistsAsync(input.AzureResourceGroup, input.AzureVmLocation.ToString());

                // Create input queue
                string inputQueueName = GetInputQueueName(virtualMachineName);
                QueueConnectionInfo inputQueueConnectionInfo = await CreateQueue(input, logger, virtualMachineName, inputQueueName);

                string vmInItScript = await GetVmInitScriptAsync(
                        virtualMachineName,
                        input,
                        inputQueueConnectionInfo,
                        logger);

                var parameters = new Dictionary<string, Dictionary<string, object>>()
                {
                    { "adminUserName", new Dictionary<string, object>() { { Key, "cloudenv" } } },
                    { "adminPassword", new Dictionary<string, object>() { { Key, Guid.NewGuid() } } },
                    { "vmSetupScript", new Dictionary<string, object>() { { Key, vmInItScript } } },
                    { "location", new Dictionary<string, object>() { { Key, input.AzureVmLocation.ToString() } } },
                    { "virtualMachineName", new Dictionary<string, object>() { { Key, virtualMachineName } } },
                    { "virtualMachineRG", new Dictionary<string, object>() { { Key, input.AzureResourceGroup } } },
                    { "virtualMachineSize", new Dictionary<string, object>() { { Key, input.AzureSkuName } } },
                    { "networkInterfaceName", new Dictionary<string, object>() { { Key, GetNetworkInterfaceName(virtualMachineName) } } },
                    { "resourceTags", new Dictionary<string, object>() { { Key, resourceTags } } },
                };

                // Create virtual machine
                var result = await azure.Deployments.Define(deploymentName)
                    .WithExistingResourceGroup(input.AzureResourceGroup)
                    .WithTemplate(VmTemplateJson)
                    .WithParameters(JsonConvert.SerializeObject(parameters))
                    .WithMode(Microsoft.Azure.Management.ResourceManager.Fluent.Models.DeploymentMode.Incremental)
                    .BeginCreateAsync();

                var azureResourceInfo = new AzureResourceInfo(input.AzureSubscription, input.AzureResourceGroup, virtualMachineName);
                return (OperationState.InProgress, new NextStageInput(result.Name, azureResourceInfo));
            }
            catch (Exception ex)
            {
                logger.LogException("linux_virtual_machine_manager_begin_create_error", ex);
                return (OperationState.Failed, default);
            }
        }

        /// <inheritdoc/>
        public async Task<(OperationState, NextStageInput)> CheckCreateComputeStatusAsync(NextStageInput input, IDiagnosticsLogger logger)
        {
            try
            {
                var azure = await clientFactory.GetAzureClientAsync(input.AzureResourceInfo.SubscriptionId);
                var deployment = await azure.Deployments.GetByResourceGroupAsync(input.AzureResourceInfo.ResourceGroup, input.TrackingId);
                return (ParseResult(deployment.ProvisioningState), new NextStageInput(input.TrackingId, input.AzureResourceInfo));
            }
            catch (Exception ex)
            {
                logger.LogException("linux_virtual_machine_manager_check_create_compute_error", ex);
                if (input.RetryAttempt < 5)
                {
                    return (OperationState.InProgress, new NextStageInput(input.TrackingId, input.AzureResourceInfo, input.RetryAttempt + 1));
                }

                return (OperationState.Failed, default);
            }
        }

        /// <inheritdoc/>
        public async Task<(OperationState, int)> StartComputeAsync(VirtualMachineProviderStartComputeInput input, int retryAttempt, IDiagnosticsLogger logger)
        {
            try
            {
                // create queue message
                var jobParameters = new Dictionary<string, string>();
                foreach (var kvp in input.VmInputParams)
                {
                    jobParameters.Add(kvp.Key, kvp.Value);
                }

                jobParameters.Add("storageAccountName", input.FileShareConnection.StorageAccountName);
                jobParameters.Add("storageAccountKey", input.FileShareConnection.StorageAccountKey);
                jobParameters.Add("storageShareName", input.FileShareConnection.StorageShareName);
                jobParameters.Add("storageFileName", input.FileShareConnection.StorageFileName);

                // Temporary: Add sku so the vm agent can limit memory on DS4_v3 VMs.
                jobParameters.Add("skuName", input.SkuName);

                var queueMessage = new QueueMessage
                {
                    Command = "StartEnvironment",
                    Parameters = jobParameters,
                };

                var message = new CloudQueueMessage(JsonConvert.SerializeObject(queueMessage));
                var queue = await GetQueueClientAsync(input.Location, GetInputQueueName(input.AzureResourceInfo.Name), logger);

                // post message to queue
                queue.AddMessage(message);

                return (OperationState.Succeeded, 0);
            }
            catch (Exception ex)
            {
                logger.LogException("linux_virtual_machine_manager_start_compute_error", ex);

                if (retryAttempt < 5)
                {
                    return (OperationState.InProgress, retryAttempt + 1);
                }

                return (OperationState.Failed, retryAttempt);
            }
        }

        /// <inheritdoc/>
        public async Task<(OperationState, int)> ShutdownComputeAsync(VirtualMachineProviderShutdownInput input, int retryAttempt, IDiagnosticsLogger logger)
        {
            try
            {
                var vmAgentQueueMessage = new QueueMessage
                {
                    Command = "ShutdownEnvironment",
                    Id = input.EnvironmentId,
                };

                var message = new CloudQueueMessage(JsonConvert.SerializeObject(vmAgentQueueMessage));
                var queue = await GetQueueClientAsync(input.AzureVmLocation, GetInputQueueName(input.AzureResourceInfo.Name), logger);

                // Push the message to queue.
                queue.AddMessage(message);

                return (OperationState.Succeeded, 0);
            }
            catch (Exception ex)
            {
                logger.LogException("linux_virtual_machine_manager_shutdown_compute_error", ex);

                if (retryAttempt < 5)
                {
                    return (OperationState.InProgress, retryAttempt + 1);
                }

                return (OperationState.Failed, retryAttempt);
            }
        }

        /// <inheritdoc/>
        public async Task<(OperationState, NextStageInput)> BeginDeleteComputeAsync(VirtualMachineProviderDeleteInput input, IDiagnosticsLogger logger)
        {
            try
            {
                var vmName = input.AzureResourceInfo.Name;
                var azure = await clientFactory.GetAzureClientAsync(input.AzureResourceInfo.SubscriptionId);
                var resourceGroup = input.AzureResourceInfo.ResourceGroup;
                var linuxVM = await azure.VirtualMachines
                                  .GetByResourceGroupAsync(resourceGroup, vmName);

                var vmDeletionState = OperationState.Succeeded;
                if (linuxVM != null)
                {
                    var computeClient = await clientFactory.GetComputeManagementClient(input.AzureResourceInfo.SubscriptionId);
                    await computeClient.VirtualMachines.BeginDeleteAsync(resourceGroup, vmName);
                    vmDeletionState = OperationState.InProgress;
                }

                // Save resource state for continuation token.
                var resourcesToBeDeleted = new Dictionary<string, VmResourceState>();
                resourcesToBeDeleted.Add(VmNameKey, (vmName, vmDeletionState));
                resourcesToBeDeleted.Add(NicNameKey, (GetNetworkInterfaceName(vmName), OperationState.NotStarted));
                resourcesToBeDeleted.Add(NsgNameKey, (GetNetworkSecurityGroupName(vmName), OperationState.NotStarted));
                resourcesToBeDeleted.Add(VnetNameKey, (GetVnetName(vmName), OperationState.NotStarted));
                resourcesToBeDeleted.Add(DiskNameKey, (GetOsDiskName(vmName), OperationState.NotStarted));
                resourcesToBeDeleted.Add(InputQueueNameKey, (GetInputQueueName(vmName), OperationState.NotStarted));
                AddOutputQueue(vmName, resourcesToBeDeleted);

                return (OperationState.InProgress, new NextStageInput(
                    CreateVmDeletionTrackingId(input.AzureVmLocation, resourcesToBeDeleted),
                    input.AzureResourceInfo));
            }
            catch (Exception ex)
            {
                logger.LogException("linux_virtual_machine_manager_begin_delete_error", ex);
                return (OperationState.Failed, default);
            }
        }

        /// <inheritdoc/>
        public async Task<(OperationState, NextStageInput)> CheckDeleteComputeStatusAsync(NextStageInput input, IDiagnosticsLogger logger)
        {
            try
            {
                var (computeVmLocation, resourcesToBeDeleted) = JsonConvert
                .DeserializeObject<(AzureLocation, Dictionary<string, VmResourceState>)>(input.TrackingId);
                string resourceGroup = input.AzureResourceInfo.ResourceGroup;
                var azure = await clientFactory.GetAzureClientAsync(input.AzureResourceInfo.SubscriptionId);
                var networkClient = await clientFactory.GetNetworkManagementClient(input.AzureResourceInfo.SubscriptionId);
                var computeClient = await clientFactory.GetComputeManagementClient(input.AzureResourceInfo.SubscriptionId);

                if (resourcesToBeDeleted[VmNameKey].State != OperationState.Succeeded)
                {
                    // Check if virtual machine deletion is complete
                    var linuxVM = await azure.VirtualMachines
                              .GetByResourceGroupAsync(resourceGroup, resourcesToBeDeleted[VmNameKey].Name);
                    if (linuxVM != null)
                    {
                        return (OperationState.InProgress, input);
                    }
                    else
                    {
                        resourcesToBeDeleted[VmNameKey] = (resourcesToBeDeleted[VmNameKey].Name, OperationState.Succeeded);
                    }
                }

                // Virtual machine is deleted, delete the remaining resources
                var taskList = new List<Task>();

                taskList.Add(
                CheckResourceStatus(
                    (resourceName) => DeleteQueueAsync(computeVmLocation, resourceName, logger),
                    (resourceName) => QueueExistsAync(computeVmLocation, resourceName, logger),
                    resourcesToBeDeleted,
                    InputQueueNameKey));

                DeleteOutputQueue(logger, computeVmLocation, resourcesToBeDeleted, taskList);

                taskList.Add(
                CheckResourceStatus(
                    (resourceName) => computeClient.Disks.BeginDeleteAsync(resourceGroup, resourceName),
                    (resourceName) => azure.Disks.GetByResourceGroupAsync(resourceGroup, resourceName),
                    resourcesToBeDeleted,
                    DiskNameKey));

                taskList.Add(
                    CheckResourceStatus(
                      (resourceName) => networkClient.NetworkInterfaces.BeginDeleteAsync(resourceGroup, resourceName),
                      (resourceName) => azure.NetworkInterfaces.GetByResourceGroupAsync(resourceGroup, resourceName),
                      resourcesToBeDeleted,
                      NicNameKey));

                if (resourcesToBeDeleted[NicNameKey].State == OperationState.Succeeded)
                {
                    taskList.Add(
                        CheckResourceStatus(
                        (resourceName) => networkClient.NetworkSecurityGroups.BeginDeleteAsync(resourceGroup, resourceName),
                        (resourceName) => azure.NetworkSecurityGroups.GetByResourceGroupAsync(resourceGroup, resourceName),
                        resourcesToBeDeleted,
                        NsgNameKey));

                    taskList.Add(
                        CheckResourceStatus(
                        (resourceName) => networkClient.VirtualNetworks.BeginDeleteAsync(resourceGroup, resourceName),
                        (resourceName) => azure.Networks.GetByResourceGroupAsync(resourceGroup, resourceName),
                        resourcesToBeDeleted,
                        VnetNameKey));
                }

                await Task.WhenAll(taskList);
                var nextStageInput = new NextStageInput(CreateVmDeletionTrackingId(computeVmLocation, resourcesToBeDeleted), input.AzureResourceInfo);
                var resultState = GetFinalState(resourcesToBeDeleted);
                return (resultState, nextStageInput);
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

                logger.LogErrorWithDetail("linux_virtual_machine_manager_check_delete_compute_error", s.ToString());
                NextStageInput nextStageInput = new NextStageInput(input.TrackingId, input.AzureResourceInfo, input.RetryAttempt + 1);
                if (input.RetryAttempt < 5)
                {
                    return (OperationState.InProgress, nextStageInput);
                }

                return (OperationState.Failed, nextStageInput);
            }
        }

        [Conditional("DEBUG")]
        private void AddOutputQueue(string vmName, Dictionary<string, VmResourceState> resourcesToBeDeleted)
        {
            resourcesToBeDeleted.Add(OutputQueueNameKey, (GetOutputQueueName(vmName), OperationState.NotStarted));
        }

        [Conditional("DEBUG")]
        private void DeleteOutputQueue(IDiagnosticsLogger logger, AzureLocation computeVmLocation, Dictionary<string, VmResourceState> resourcesToBeDeleted, List<Task> taskList)
        {
            taskList.Add(
            CheckResourceStatus(
               (resourceName) => DeleteQueueAsync(computeVmLocation, resourceName, logger),
               (resourceName) => QueueExistsAync(computeVmLocation, resourceName, logger),
               resourcesToBeDeleted,
               OutputQueueNameKey));
        }

        private async Task<(OperationState, QueueConnectionInfo, int)> GetVirtualMachineInputQueueConnectionInfoAsync(
            AzureLocation location,
            string queueName,
            int retryAttempCount,
            IDiagnosticsLogger logger)
        {
            try
            {
                var queueClient = await GetQueueClientAsync(location, queueName, logger);
                var sas = queueClient.GetSharedAccessSignature(new SharedAccessQueuePolicy()
                {
                    Permissions = SharedAccessQueuePermissions.ProcessMessages,
                    SharedAccessExpiryTime = DateTime.UtcNow.AddDays(365),
                });

                return (OperationState.Succeeded, new QueueConnectionInfo(queueClient.Name, queueClient.ServiceClient.BaseUri.ToString(), sas), 0);
            }
            catch (Exception ex)
            {
                logger.LogException("linux_virtual_machine_manager_get_input_queue_url_error", ex);
                if (retryAttempCount < 5)
                {
                    return (OperationState.InProgress, default, retryAttempCount + 1);
                }

                return (OperationState.Failed, default, 0);
            }
        }

        private async Task<(OperationState, QueueConnectionInfo, int)> GetVirtualMachineOutputQueueConnectionInfoAsync(
          AzureLocation location,
          string queueName,
          int retryAttempCount,
          IDiagnosticsLogger logger)
        {
            try
            {
                var queueClient = await GetQueueClientAsync(location, queueName, logger);
                var sas = queueClient.GetSharedAccessSignature(new SharedAccessQueuePolicy()
                {
                    Permissions = SharedAccessQueuePermissions.Add,
                    SharedAccessExpiryTime = DateTime.UtcNow.AddDays(365),
                });

                return (OperationState.Succeeded, new QueueConnectionInfo(queueClient.Name, queueClient.ServiceClient.BaseUri.ToString(), sas), 0);
            }
            catch (Exception ex)
            {
                logger.LogException("linux_virtual_machine_manager_get_output_queue_url_error", ex);
                if (retryAttempCount < 5)
                {
                    return (OperationState.InProgress, default, retryAttempCount + 1);
                }

                return (OperationState.Failed, default, 0);
            }
        }

        private async Task<QueueConnectionInfo> CreateQueue(VirtualMachineProviderCreateInput input, IDiagnosticsLogger logger, string virtualMachineName, string queueName)
        {
            var queue = await GetQueueClientAsync(input.AzureVmLocation, queueName, logger);
            var queueCreated = await queue.CreateIfNotExistsAsync();
            if (!queueCreated)
            {
                throw new VirtualMachineException($"Failed to create queue for virtual machine {virtualMachineName}");
            }

            // Get queue sas url
            var queueResult = queueName.EndsWith("-input-queue")
                ? await GetVirtualMachineInputQueueConnectionInfoAsync(input.AzureVmLocation, queue.Name, 0, logger)
                : await GetVirtualMachineOutputQueueConnectionInfoAsync(input.AzureVmLocation, queue.Name, 0, logger);
            if (queueResult.Item1 != OperationState.Succeeded)
            {
                throw new VirtualMachineException($"Failed to get sas token for virtual machine input queue {queue.Uri}");
            }

            var queueConnectionInfo = queueResult.Item2;
            return queueConnectionInfo;
        }

        private static OperationState GetFinalState(Dictionary<string, VmResourceState> resourcesToBeDeleted)
        {
            if (resourcesToBeDeleted.Any(r => r.Value.State == OperationState.InProgress || r.Value.State == OperationState.NotStarted))
            {
                return OperationState.InProgress;
            }

            return OperationState.Succeeded;
        }

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

        private static OperationState ParseResult(string provisioningState)
        {
            if (provisioningState.Equals(OperationState.Succeeded.ToString(), StringComparison.OrdinalIgnoreCase)
           || provisioningState.Equals(OperationState.Failed.ToString(), StringComparison.OrdinalIgnoreCase)
           || provisioningState.Equals(OperationState.Cancelled.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return provisioningState.ToEnum<OperationState>();
            }

            return OperationState.InProgress;
        }

        private static string GetVmTemplate()
        {
            return GetEmbeddedResource("template_vm.json");
        }

        private static string GetCustomScriptForVmAssign(string scriptName, VirtualMachineProviderStartComputeInput input)
        {
            var scriptString = GetEmbeddedResource(scriptName);
            scriptString = AddParamsToScript(input, scriptString);
            return scriptString.ToBase64Encoded();
        }

        private static string AddParamsToScript(VirtualMachineProviderStartComputeInput input, string scriptString)
        {
            var camelCaseSerializer = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            var storageParams = JsonConvert.SerializeObject(input.FileShareConnection, Formatting.None, camelCaseSerializer);
            scriptString = scriptString.Replace("SCRIPT_PARAM_STORAGE=''", $"SCRIPT_PARAM_STORAGE='{storageParams}'");
            var envParams = JsonConvert.SerializeObject(input.VmInputParams);
            scriptString = scriptString.Replace("SCRIPT_PARAM_CONTAINER_ENV_VARS=''", $"SCRIPT_PARAM_CONTAINER_ENV_VARS='{envParams}'");
            return scriptString;
        }

        private static string GetEmbeddedResource(string resourceName)
        {
            var namespaceString = typeof(LinuxVirtualMachineManager).Namespace;
            var fullResourceName = $"{namespaceString}.Templates.Linux.{resourceName}";
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(fullResourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string result = reader.ReadToEnd();
                return result;
            }
        }

        private static string CreateVmDeletionTrackingId(AzureLocation computeVmLocation, Dictionary<string, VmResourceState> resourcesToBeDeleted)
        {
            return JsonConvert.SerializeObject((computeVmLocation, resourcesToBeDeleted));
        }

        private string ReplaceParams(
            string vmToken,
            string vmAgentBlobUrl,
            QueueConnectionInfo queueConnectionInfo,
            string resourceId,
            string frontEndDnsHostName,
            string initScript)
        {
            return initScript
                .Replace("__REPLACE_INPUT_QUEUE_NAME__", queueConnectionInfo.Name)
                .Replace("__REPLACE_INPUT_QUEUE_URL__", queueConnectionInfo.Url)
                .Replace("__REPLACE_INPUT_QUEUE_SASTOKEN__", queueConnectionInfo.SasToken)
                .Replace("__REPLACE_VMTOKEN__", vmToken)
                .Replace("__REPLACE_VMAGENT_BLOB_URl__", vmAgentBlobUrl)
                .Replace("__REPLACE_RESOURCEID__", resourceId)
                .Replace("__REPLACE_FRONTEND_SERVICE_DNS_HOST_NAME__", frontEndDnsHostName);
        }

        private async Task<CloudQueue> GetQueueClientAsync(AzureLocation location, string queueName, IDiagnosticsLogger logger)
        {
            var (accountName, accountKey) = await controlPlaneAzureResourceAccessor.GetStampStorageAccountForComputeQueuesAsync(location, logger);
            var storageCredentials = new StorageCredentials(accountName, accountKey);
            var storageAccount = new CloudStorageAccount(storageCredentials, useHttps: true);
            var queueClient = new CloudQueueClient(storageAccount.QueueStorageUri, storageCredentials);
            var queue = queueClient.GetQueueReference(queueName);
            return queue;
        }

        private async Task DeleteQueueAsync(AzureLocation location, string queueName, IDiagnosticsLogger logger)
        {
            var queue = await GetQueueClientAsync(location, queueName, logger);
            await queue.DeleteIfExistsAsync();
        }

        private async Task<object> QueueExistsAync(AzureLocation location, string queueName, IDiagnosticsLogger logger)
        {
            var queue = await GetQueueClientAsync(location, queueName, logger);
            var queueExists = await queue.ExistsAsync();
            return queueExists ? new object() : default;
        }

        private async Task<string> GetVmInitScriptAsync(
            string virtualMachineName,
            VirtualMachineProviderCreateInput input,
            QueueConnectionInfo inputQueueConnectionInfo,
            IDiagnosticsLogger logger)
        {
            var initScript = GetEmbeddedResource("vm_init.sh");
            initScript = ReplaceParams(
                input.VMToken,
                input.VmAgentBlobUrl,
                inputQueueConnectionInfo,
                input.ResourceId,
                input.FrontDnsHostName,
                initScript);

            if (useOutputQueue)
            {
                string outputQueueName = GetOutputQueueName(virtualMachineName);
                QueueConnectionInfo outputQueueConnectionInfo = await CreateQueue(input, logger, virtualMachineName, outputQueueName);
                initScript = initScript
                                .Replace("SCRIPT_PARAM_VM_USE_OUTPUT_QUEUE=0", "SCRIPT_PARAM_VM_USE_OUTPUT_QUEUE=1")
                                .Replace("__REPLACE_OUTPUT_QUEUE_NAME__", outputQueueConnectionInfo.Name)
                                .Replace("__REPLACE_OUTPUT_QUEUE_URL__", outputQueueConnectionInfo.Url)
                                .Replace("__REPLACE_OUTPUT_QUEUE_SASTOKEN__", outputQueueConnectionInfo.SasToken);
            }

            return initScript.ToBase64Encoded();
        }

        private string GetOutputQueueName(string virtualMachineName)
        {
            return $"{virtualMachineName}-output-queue";
        }

        private static string GetInputQueueName(string virtualMachineName)
        {
            return $"{virtualMachineName.ToLowerInvariant()}-input-queue";
        }

        private static string GetOsDiskName(string vmName)
        {
            return $"{vmName}-disk";
        }

        private static string GetVnetName(string vmName)
        {
            return $"{vmName}-vnet";
        }

        private static string GetNetworkSecurityGroupName(string vmName)
        {
            return $"{vmName}-nsg";
        }

        private static string GetNetworkInterfaceName(string vmName)
        {
            return $"{vmName}-nic";
        }
    }
}