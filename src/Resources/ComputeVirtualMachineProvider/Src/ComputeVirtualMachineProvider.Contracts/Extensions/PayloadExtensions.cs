// <copyright file="PayloadExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts.Extensions
{
    /// <summary>
    /// Extension methods for generating payloads.
    /// </summary>
    public static class PayloadExtensions
    {
        /// <summary>
        /// VM Agent Blob URL property.
        /// </summary>
        public const string VMAgentBlobUrl = "vmAgentBlobUrl";

        private const string VMTokenTag = "vmToken";
        private const string ResourceIDTag = "resourceId";
        private const string ServiceHostTag = "serviceHostName";
        private const string InputQueueNameTag = "inputQueueName";
        private const string InputQueueUrlTag = "inputQueueUrl";
        private const string InputQueueSasTokenTag = "inputQueueSasToken";

        private const string VisualStudioInstallationDirTag = "visualStudioInstallationDir";
        private const string UserNameTag = "userName";
        private const string StorageAccountNameTag = "storageAccountName";
        private const string StorageAccountKeyTag = "storageAccountKey";
        private const string StorageShareNameTag = "storageShareName";
        private const string StorageFileNameTag = "storageFileName";
        private const string StorageFileServiceHostTag = "storageFileServiceHost";
        private const string SkuNameTag = "skuName";

        private const string RefreshVMCommand = "RefreshVM";
        private const string StartEnvironmentCommand = "StartEnvironment";
        private const string ShutdownEnvironmentCommand = "ShutdownEnvironment";
        private const string UpdateSystemCommand = "UpdateSystem";
        private const string ExportEnvironmentCommand = "ExportEnvironment";

        /// <summary>
        /// Creates queue payload for VM refresh.
        /// </summary>
        /// <param name="input">Virtual machine create input.</param>
        /// <returns>Queue payload.</returns>
        public static QueueMessage GenerateRefreshVMPayload(this VirtualMachineProviderCreateInput input)
        {
            var jobParameters = new Dictionary<string, string>
            {
                [VMTokenTag] = input.VMToken,
                [ResourceIDTag] = input.ResourceId,
                [ServiceHostTag] = input.FrontDnsHostName,
                [InputQueueNameTag] = input.QueueConnectionInfo.Name,
                [InputQueueUrlTag] = input.QueueConnectionInfo.Url,
                [InputQueueSasTokenTag] = input.QueueConnectionInfo.SasToken,
                [VMAgentBlobUrl] = input.VmAgentBlobUrl,
            };

            var queueMessage = new QueueMessage
            {
                Command = RefreshVMCommand,
                Parameters = jobParameters,
            };

            return queueMessage;
        }

        /// <summary>
        /// Creates payload for the initial Custom script extension.
        /// </summary>
        /// <param name="input">Virtual machine create input.</param>
        /// <param name="vsInstallationDirectory">Visual studio installation directory.</param>
        /// <param name="userName">User name.</param>
        /// <returns>Dictionary payload for Custom script extension.</returns>
        public static Dictionary<string, object> GenerateInitScriptParametersBlob(
            this VirtualMachineProviderCreateInput input,
            string vsInstallationDirectory,
            string userName)
        {
            var initScriptParametersBlob = new Dictionary<string, object>
            {
                [VMTokenTag] = input.VMToken,
                [ResourceIDTag] = input.ResourceId,
                [ServiceHostTag] = input.FrontDnsHostName,
                [InputQueueNameTag] = input.QueueConnectionInfo.Name,
                [InputQueueUrlTag] = input.QueueConnectionInfo.Url,
                [InputQueueSasTokenTag] = input.QueueConnectionInfo.SasToken,
                [VisualStudioInstallationDirTag] = vsInstallationDirectory,
                [UserNameTag] = userName,
            };

            return initScriptParametersBlob;
        }

        /// <summary>
        /// Creates queue payload for environment start.
        /// </summary>
        /// <param name="startComputeInput">Virtual machine start compute input.</param>
        /// <returns>Queue payload.</returns>
        public static QueueMessage GenerateStartEnvironmentPayload(this VirtualMachineProviderStartComputeInput startComputeInput)
        {
            var jobParameters = CreateJobParameters(startComputeInput);

            var queueMessage = new QueueMessage
            {
                Command = StartEnvironmentCommand,
                Parameters = jobParameters,
                UserSecrets = startComputeInput.UserSecrets,
                DevContainer = startComputeInput.DevContainer,
            };

            // Temporary: Add sku so the vm agent can limit memory on DS4_v3 VMs.
            jobParameters.Add(SkuNameTag, startComputeInput.SkuName);

            return queueMessage;
        }

        /// <summary>
        /// Creates queue payload for environment export.
        /// </summary>
        /// <param name="startComputeInput">Virtual machine start compute input.</param>
        /// <returns>Queue payload.</returns>
        public static QueueMessage GenerateExportEnvironmentPayload(this VirtualMachineProviderStartComputeInput startComputeInput)
        {
            var jobParameters = CreateJobParameters(startComputeInput);

            var queueMessage = new QueueMessage
            {
                Command = ExportEnvironmentCommand,
                Parameters = jobParameters,
                UserSecrets = startComputeInput.UserSecrets,
            };

            return queueMessage;
        }

        /// <summary>
        /// Creates queue payload for environment shutdown.
        /// </summary>
        /// <param name="input">Virtual machine shutdown input.</param>
        /// <returns>Queue payload.</returns>
        public static QueueMessage GenerateShutdownEnvironmentPayload(this VirtualMachineProviderShutdownInput input)
        {
            var queueMessage = new QueueMessage
            {
                Command = ShutdownEnvironmentCommand,
                Id = input.EnvironmentId.ToString(),
                Parameters = new Dictionary<string, string>
                {
                    ["computeResourceId"] = input.ComputeResourceId,
                },
            };

            return queueMessage;
        }

        /// <summary>
        /// Creates queue payload for system update.
        /// </summary>
        /// <param name="environmentId">The Environment ID.</param>
        /// <returns>Queue payload.</returns>
        public static QueueMessage GenerateUpdateSystemPayload(this Guid environmentId)
        {
            var queueMessage = new QueueMessage
            {
                Command = UpdateSystemCommand,
                Id = environmentId.ToString(),
            };

            return queueMessage;
        }

        private static Dictionary<string, string> CreateJobParameters(this VirtualMachineProviderStartComputeInput startComputeInput)
        {
            var jobParameters = new Dictionary<string, string>();
            foreach (var kvp in startComputeInput.VmInputParams)
            {
                jobParameters.Add(kvp.Key, kvp.Value);
            }

            if (startComputeInput.FileShareConnection != null)
            {
                jobParameters.Add(StorageAccountNameTag, startComputeInput.FileShareConnection.StorageAccountName);
                jobParameters.Add(StorageAccountKeyTag, startComputeInput.FileShareConnection.StorageAccountKey);
                jobParameters.Add(StorageShareNameTag, startComputeInput.FileShareConnection.StorageShareName);
                jobParameters.Add(StorageFileNameTag, startComputeInput.FileShareConnection.StorageFileName);
                jobParameters.Add(StorageFileServiceHostTag, startComputeInput.FileShareConnection.StorageFileServiceHost);
            }

            return jobParameters;
        }
    }
}
