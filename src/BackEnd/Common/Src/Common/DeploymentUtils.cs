// <copyright file="DeploymentUtils.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common
{
    /// <summary>
    /// Common Azure resource deployment utilities.
    /// </summary>
    public static class DeploymentUtils
    {
        private static readonly int MaxLength = 1000;

        /// <summary>
        /// Create exception for ARM deployment failures.
        /// </summary>
        /// <param name="deployment">deployment object.</param>
        /// <returns>Error details for ARM Deployment.</returns>
        public static async Task<string> ExtractDeploymentErrors(IDeployment deployment)
        {
            var operations = await deployment.DeploymentOperations.ListAsync();
            foreach (var op in operations)
            {
                if (op.ProvisioningState == "Failed" && op.TargetResource != null)
                {
                    // Log ResourceId, StatusCode and Status Message
                    var errorMessage = op.StatusMessage.ToString();
                    var startIndex = (errorMessage.Length > MaxLength) ? errorMessage.Length - MaxLength : 0;
                    errorMessage = errorMessage.Substring(startIndex);

                    var errorDetails = new DeploymentErrorDetails()
                    {
                        Id = op.TargetResource?.Id,
                        StatusCode = op.StatusCode,
                        ErrorMessage = errorMessage,
                    };

                    var opDetailsString = JsonConvert.SerializeObject(errorDetails);
                    return opDetailsString;
                }
            }

            // No errors found
            return string.Empty;
        }

        /// <summary>
        /// Execute the specified begin/status check operation for an ARM deployment.
        /// </summary>
        /// <typeparam name="T">Continuation input object type for the ARM deployment.</typeparam>
        /// <param name="input">The continuation input.</param>
        /// <param name="logger">The IDiagnosticsLogger.</param>
        /// <param name="beginOperation">Callback to begin an ARM deployment.</param>
        /// <param name="checkOperationStatus">Callback to check ARM deployment status.</param>
        /// <returns>Operation result.</returns>
        public static async Task<(AzureResourceInfo, OperationState, string)> ExecuteOperationAsync<T>(
            T input,
            IDiagnosticsLogger logger,
            Func<T, IDiagnosticsLogger, Task<(OperationState, NextStageInput)>> beginOperation,
            Func<NextStageInput, IDiagnosticsLogger, Task<(OperationState, NextStageInput)>> checkOperationStatus)
            where T : ContinuationInput
        {
            OperationState resultState;
            NextStageInput nextStageInput;
            string resultContinuationToken = default;
            var continuationToken = input.ContinuationToken;

            if (string.IsNullOrEmpty(continuationToken))
            {
                (resultState, nextStageInput) = await beginOperation(input, logger);
            }
            else
            {
                // Check status of deployment request
                nextStageInput = continuationToken.ToNextStageInput();
                (resultState, nextStageInput) = await checkOperationStatus(nextStageInput, logger);
            }

            if (resultState == OperationState.InProgress)
            {
                resultContinuationToken = nextStageInput.ToJson();
            }

            return (nextStageInput?.AzureResourceInfo, resultState, resultContinuationToken);
        }

        /// <summary>
        /// Parse ARM deployment provisioning status to determine the <see cref="OperationState"/>.
        /// </summary>
        /// <param name="provisioningState">Provisioning state returned by ARM.</param>
        /// <returns><see cref="OperationState"/> representing the current state of deployment.</returns>
        public static OperationState ParseProvisioningState(string provisioningState)
        {
            if (provisioningState.Equals(OperationState.Succeeded.ToString(), StringComparison.OrdinalIgnoreCase)
           || provisioningState.Equals(OperationState.Failed.ToString(), StringComparison.OrdinalIgnoreCase)
           || provisioningState.Equals(OperationState.Cancelled.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return provisioningState.ToEnum<OperationState>();
            }

            return OperationState.InProgress;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="azure"></param>
        /// <param name="deploymentName"></param>
        /// <param name="resourceGroup"></param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public static async Task<OperationState> CheckArmResourceDeploymentState(
            IAzure azure,
            string deploymentName,
            string resourceGroup)
        {
            var deployment = await azure.Deployments.GetByResourceGroupAsync(resourceGroup, deploymentName);

            OperationState operationState = DeploymentUtils.ParseProvisioningState(deployment.ProvisioningState);
            if (operationState == OperationState.Failed)
            {
                var errorDetails = await ExtractDeploymentErrors(deployment);
                throw new DeploymentException(errorDetails);
            }

            return operationState;
        }

        /// <summary>
        /// Submit Resource deployment request to ARM.
        /// </summary>
        /// <param name="resourceGroup">resource group.</param>
        /// <param name="azure">Azure client.</param>
        /// <param name="resourceTemplate">resource Template.</param>
        /// <param name="parameters">template parameters.</param>
        /// <param name="deploymentName">deployment name.</param>
        /// <returns>result.</returns>
        public static async Task<IDeployment> BeginCreateArmResource(
            string resourceGroup,
            IAzure azure,
            string resourceTemplate,
            Dictionary<string, Dictionary<string, object>> parameters,
            string deploymentName)
        {
            return await azure.Deployments.Define(deploymentName)
                .WithExistingResourceGroup(resourceGroup)
                .WithTemplate(resourceTemplate)
                .WithParameters(JsonConvert.SerializeObject(parameters))
                .WithMode(DeploymentMode.Incremental)
                .BeginCreateAsync();
        }
    }
}
