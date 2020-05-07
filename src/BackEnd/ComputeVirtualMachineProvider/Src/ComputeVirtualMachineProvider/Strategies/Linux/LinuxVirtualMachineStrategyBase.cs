﻿// <copyright file="LinuxVirtualMachineStrategyBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine.Strategies
{
    /// <summary>
    /// LinuxVirtualMachineStrategyBase.
    /// </summary>
    public abstract class LinuxVirtualMachineStrategyBase : ICreateVirtualMachineStrategy
    {
        /// <summary>
        /// VM ssh key parameter input.
        /// </summary>
        protected const string VmPublicSshKey = "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABAQDPYyB2V9q43oWBJINIZMzJ4zrYoNptGCK9i28qxj9cS2Af/FkrVYTSSRMPJforJRVkyY/6M63TFzfIeMe6b93g9Q4nDoxyUDMJ5SEkLp3kw3caxrimLQF2yJz4QySoiqaFlhfot9bP3En9i8AQYjogQtxQqpw77RRzQOkinP5gyga5W2Ia/inNGBRwF2guqZccsOrTI2WF6dnHTB28LIxhHox/WH+0CmMKQrP8yX3bzoReXvsmm8RztC6PWb3G9FzEXK6fDdaLApSIvO/sc5MSEEdbwx7yo2phWsv96x7wY3QZtUGg+ZZnrtr/RE05xQPHm+ufh7qwbF87Ekt70h9R vsonline@machine";

        /// <summary>
        /// VM admin name parameter input.
        /// </summary>
        protected const string AdminUserName = "cloudenv";

        /// <summary>
        /// VM ssh key path parameter input.
        /// </summary>
        protected static readonly string PublicKeyPath = $"/home/{AdminUserName}/.ssh/authorized_keys";

        /// <summary>
        /// Initializes a new instance of the <see cref="LinuxVirtualMachineStrategyBase"/> class.
        /// </summary>
        /// <param name="clientFactory">azure client factory.</param>
        /// <param name="queueProvider">queue provider.</param>
        /// <param name="templateName">template name.</param>
        public LinuxVirtualMachineStrategyBase(
            IAzureClientFactory clientFactory,
            IQueueProvider queueProvider,
            string templateName)
        {
            ClientFactory = Requires.NotNull(clientFactory, nameof(clientFactory));
            QueueProvider = Requires.NotNull(queueProvider, nameof(queueProvider));
            VirtualMachineTemplateJson = GetVmTemplate(templateName);
        }

        /// <inheritdoc/>
        public string VirtualMachineTemplateJson { get; }

        /// <summary>
        /// Gets azure client provider.
        /// </summary>
        protected IAzureClientFactory ClientFactory { get; }

        /// <summary>
        /// Gets queue client provider.
        /// </summary>
        protected IQueueProvider QueueProvider { get; }

        /// <inheritdoc/>
        public async Task<(OperationState OperationState, NextStageInput NextInput)> BeginCreateVirtualMachine(
            VirtualMachineProviderCreateInput input,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(input.AzureResourceGroup, nameof(input.AzureResourceGroup));
            Requires.NotNull(input.AzureSkuName, nameof(input.AzureSkuName));
            Requires.NotNull(input.AzureVirtualMachineImage, nameof(input.AzureVirtualMachineImage));
            Requires.NotNull(input.VmAgentBlobUrl, nameof(input.VmAgentBlobUrl));
            Requires.NotNull(input.VMToken, nameof(input.VMToken));
            Requires.NotNull(input.ResourceId, nameof(input.ResourceId));
            Requires.NotNull(input.FrontDnsHostName, nameof(input.FrontDnsHostName));

            // create new VM resource name
            var virtualMachineName = GetVirtualMachineName();

            var resourceTags = input.ResourceTags;

            VirtualMachineDeploymentManager.UpdateResourceTags(input.CustomComponents, virtualMachineName, resourceTags);

            var azure = await ClientFactory.GetAzureClientAsync(input.AzureSubscription);
            await azure.CreateResourceGroupIfNotExistsAsync(input.AzureResourceGroup, input.AzureVmLocation.ToString());

            // Create input queue
            var inputQueueName = VirtualMachineResourceNames.GetInputQueueName(virtualMachineName);
            var inputQueueConnectionInfo = await QueueProvider.CreateQueue(new QueueProviderCreateInput() { AzureLocation = input.AzureVmLocation, QueueName = inputQueueName, },  logger);

            string vmInitScript = GetVmInitScriptAsync(
                    input,
                    inputQueueConnectionInfo);

            var parameters = GetVMParameters(input, virtualMachineName, resourceTags, vmInitScript);

            var deploymentName = $"Create-LinuxVm-{virtualMachineName}";

            // Create virtual machine
            var result = await DeploymentUtils.BeginCreateArmResource(
                input.AzureResourceGroup,
                azure,
                VirtualMachineTemplateJson,
                parameters,
                deploymentName);

            var azureResourceInfo = new AzureResourceInfo(input.AzureSubscription, input.AzureResourceGroup, virtualMachineName);

            return (OperationState.InProgress, new NextStageInput(result.Name, azureResourceInfo));
        }

        /// <inheritdoc/>
        public abstract bool Accepts(VirtualMachineProviderCreateInput input);

        /// <summary>
        /// Get tamplate parameters.
        /// </summary>
        /// <param name="input">vm input.</param>
        /// <param name="virtualMachineName">vm name.</param>
        /// <param name="resourceTags">resource tags.</param>
        /// <param name="vmInitScript">vm setup script.</param>
        /// <returns>result.</returns>
        protected abstract Dictionary<string, Dictionary<string, object>> GetVMParameters(
           VirtualMachineProviderCreateInput input,
           string virtualMachineName,
           IDictionary<string, string> resourceTags,
           string vmInitScript);

        private string GetVmTemplate(string templateName)
        {
            var fullyQualifiedResourceName = GetFullyQualifiedResourceName(templateName);
            return CommonUtils.GetEmbeddedResourceContent(fullyQualifiedResourceName);
        }

        private string GetVirtualMachineName()
        {
            return Guid.NewGuid().ToString();
        }

        private string GetVmInitScriptAsync(
            VirtualMachineProviderCreateInput input,
            QueueConnectionInfo inputQueueConnectionInfo)
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

            return initScript.ToBase64Encoded();
        }

        private string GetFullyQualifiedResourceName(string resourceName)
        {
            var namespaceString = typeof(VirtualMachineDeploymentManager).Namespace;
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
    }
}