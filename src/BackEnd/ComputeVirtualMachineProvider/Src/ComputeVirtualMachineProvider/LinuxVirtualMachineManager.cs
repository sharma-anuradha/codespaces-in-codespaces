// <copyright file="LinuxVirtualMachineManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine
{
    /// <summary>
    /// Create, update and deletes Azure virtual machines.
    /// </summary>
    public class LinuxVirtualMachineManager : VirtualMachineManagerBase
    {
        private const string VmPublicSshKey = "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABAQDPYyB2V9q43oWBJINIZMzJ4zrYoNptGCK9i28qxj9cS2Af/FkrVYTSSRMPJforJRVkyY/6M63TFzfIeMe6b93g9Q4nDoxyUDMJ5SEkLp3kw3caxrimLQF2yJz4QySoiqaFlhfot9bP3En9i8AQYjogQtxQqpw77RRzQOkinP5gyga5W2Ia/inNGBRwF2guqZccsOrTI2WF6dnHTB28LIxhHox/WH+0CmMKQrP8yX3bzoReXvsmm8RztC6PWb3G9FzEXK6fDdaLApSIvO/sc5MSEEdbwx7yo2phWsv96x7wY3QZtUGg+ZZnrtr/RE05xQPHm+ufh7qwbF87Ekt70h9R vsonline@machine";
        private const string AdminUserName = "cloudenv";
        private static readonly string PublicKeyPath = $"/home/{AdminUserName}/.ssh/authorized_keys";

        /// <summary>
        /// Initializes a new instance of the <see cref="LinuxVirtualMachineManager"/> class.
        /// </summary>
        /// <param name="clientFactory">Builds Azure clients.</param>
        /// <param name="controlPlaneAzureResourceAccessor">Control plane azure resource accessor.</param>
        public LinuxVirtualMachineManager(
            IAzureClientFactory clientFactory,
            IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor)
            : base(clientFactory, controlPlaneAzureResourceAccessor)
        {
        }

        /// <inheritdoc/>
        public override bool Accepts(ComputeOS computeOS)
        {
            return computeOS == ComputeOS.Linux;
        }

        /// <inheritdoc/>
        public override async Task<(OperationState OperationState, NextStageInput NextInput)> BeginCreateComputeAsync(VirtualMachineProviderCreateInput input, IDiagnosticsLogger logger)
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
                var azure = await ClientFactory.GetAzureClientAsync(input.AzureSubscription);
                await azure.CreateResourceGroupIfNotExistsAsync(input.AzureResourceGroup, input.AzureVmLocation.ToString());

                // Create input queue
                var inputQueueName = GetInputQueueName(virtualMachineName);
                var inputQueueConnectionInfo = await CreateQueue(input, logger, virtualMachineName, inputQueueName);

                string vmInitScript = await GetVmInitScriptAsync(
                        virtualMachineName,
                        input,
                        inputQueueConnectionInfo,
                        logger);

                var parameters = new Dictionary<string, Dictionary<string, object>>()
                {
                    { "adminUserName", new Dictionary<string, object>() { { Key, AdminUserName } } },
                    { "vmSetupScript", new Dictionary<string, object>() { { Key, vmInitScript } } },
                    { "adminPublicKeyPath", new Dictionary<string, object>() { { Key, PublicKeyPath } } },
                    { "adminPublicKey", new Dictionary<string, object>() { { Key, VmPublicSshKey } } },
                    { "location", new Dictionary<string, object>() { { Key, input.AzureVmLocation.ToString() } } },
                    { "virtualMachineName", new Dictionary<string, object>() { { Key, virtualMachineName } } },
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
                throw;
            }
        }

        /// <inheritdoc/>
        protected override string GetVmTemplate()
        {
            var resourceName = "template_vm.json";
            var fullyQualifiedResourceName = GetFullyQualifiedResourceName(resourceName);
            return CommonUtils.GetEmbeddedResourceContent(fullyQualifiedResourceName);
        }

        private string GetFullyQualifiedResourceName(string resourceName)
        {
            var namespaceString = typeof(LinuxVirtualMachineManager).Namespace;
            return $"{namespaceString}.Templates.Linux.{resourceName}";
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
                .Replace("__REPLACE_VM_PUBLIC_KEY_PATH__", PublicKeyPath)
                .Replace("__REPLACE_FRONTEND_SERVICE_DNS_HOST_NAME__", frontEndDnsHostName);
        }

        private async Task<string> GetVmInitScriptAsync(
            string virtualMachineName,
            VirtualMachineProviderCreateInput input,
            QueueConnectionInfo inputQueueConnectionInfo,
            IDiagnosticsLogger logger)
        {
            var fullyQualifiedResourceName = GetFullyQualifiedResourceName("vm_init.sh");
            var initScript = CommonUtils.GetEmbeddedResourceContent(fullyQualifiedResourceName);
            initScript = ReplaceParams(
                input.VMToken,
                input.VmAgentBlobUrl,
                inputQueueConnectionInfo,
                input.ResourceId,
                input.FrontDnsHostName,
                initScript);

            if (UseOutputQueue)
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
    }
}