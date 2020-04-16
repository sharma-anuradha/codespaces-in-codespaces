// <copyright file="KeyVaultProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Models;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider
{
    /// <inheritdoc/>
    [LoggingBaseName("keyvault_provider")]
    public class KeyVaultProvider : IKeyVaultProvider
    {
        private const string Key = "value";
        private readonly TimeSpan creationRetryInterval = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyVaultProvider"/> class.
        /// </summary>
        /// <param name="clientFactory">Factory that builds Azure clients.</param>
        public KeyVaultProvider(IAzureClientFactory clientFactory)
        {
            AzureClientFactory = clientFactory;
            KeyVaultTemplate = GetKeyVaultTemplate();
        }

        private IAzureClientFactory AzureClientFactory { get; }

        private string KeyVaultTemplate { get; }

        private string LoggingBaseName => GetType().GetLogMessageBaseName();

        /// <inheritdoc/>
        public async Task<KeyVaultProviderCreateResult> CreateAsync(KeyVaultProviderCreateInput input, IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
                $"{LoggingBaseName}_create",
                async (childLogger) =>
                {
                    string resultContinuationToken = default;
                    OperationState resultState;
                    AzureResourceInfo azureResourceInfo = default;

                    (azureResourceInfo, resultState, resultContinuationToken) = await DeploymentUtils.ExecuteOperationAsync(
                                input,
                                childLogger,
                                BeginCreateKeyVaultAsync,
                                CheckCreateKeyVaultStatusAsync);

                    var result = new KeyVaultProviderCreateResult()
                    {
                        AzureResourceInfo = azureResourceInfo,
                        Status = resultState,
                        RetryAfter = creationRetryInterval,
                        NextInput = input.BuildNextInput(resultContinuationToken),
                    };

                    return result;
                },
                (ex, childLogger) =>
                {
                    var result = new KeyVaultProviderCreateResult() { Status = OperationState.Failed, ErrorReason = ex.Message };
                    return Task.FromResult(result);
                },
                swallowException: true);
        }

        /// <inheritdoc/>
        public Task<KeyVaultProviderDeleteResult> DeleteAsync(KeyVaultProviderDeleteInput input, IDiagnosticsLogger logger)
        {
            throw new System.NotImplementedException();
        }

        private async Task<(OperationState OperationState, NextStageInput NextInput)> BeginCreateKeyVaultAsync(KeyVaultProviderCreateInput input, IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync($"{GetType().GetLogMessageBaseName()}_begin_create_keyvault", async (childLogger) =>
            {
                Requires.NotNull(input.AzureResourceGroup, nameof(input.AzureResourceGroup));
                Requires.NotNull(input.AzureSkuName, nameof(input.AzureSkuName));
                Requires.NotNull(input.ResourceId, nameof(input.ResourceId));
                Requires.NotNull(input.AzureTenantId, nameof(input.AzureTenantId));
                Requires.NotNull(input.AzureObjectId, nameof(input.AzureObjectId));
                Requires.NotNull(input.ResourceId, nameof(input.ResourceId));

                var azure = await AzureClientFactory.GetAzureClientAsync(Guid.Parse(input.AzureSubscriptionId));
                await azure.CreateResourceGroupIfNotExistsAsync(input.AzureResourceGroup, input.AzureLocation.ToString());

                var keyVaultName = GenerateKeyVaultName();
                var deploymentName = $"Create-KeyVault-{keyVaultName}";
                var resourceTags = input.ResourceTags;

                var parameters = new Dictionary<string, Dictionary<string, object>>()
                {
                    { "location", new Dictionary<string, object>() { { Key, input.AzureLocation.ToString() } } },
                    { "skuName", new Dictionary<string, object>() { { Key, input.AzureSkuName } } },
                    { "keyVaultName", new Dictionary<string, object>() { { Key, keyVaultName } } },
                    { "tenantId", new Dictionary<string, object>() { { Key, input.AzureTenantId } } },
                    { "objectId", new Dictionary<string, object>() { { Key, input.AzureObjectId } } },
                    { "resourceTags", new Dictionary<string, object>() { { Key, resourceTags } } },
                };

                // Create virtual machine
                var result = await azure.Deployments.Define(deploymentName)
                    .WithExistingResourceGroup(input.AzureResourceGroup)
                    .WithTemplate(KeyVaultTemplate)
                    .WithParameters(JsonConvert.SerializeObject(parameters))
                    .WithMode(Microsoft.Azure.Management.ResourceManager.Fluent.Models.DeploymentMode.Complete)
                    .BeginCreateAsync();

                var azureResourceInfo = new AzureResourceInfo(input.AzureSubscriptionId, input.AzureResourceGroup, keyVaultName);
                return (OperationState.InProgress, new NextStageInput(result.Name, azureResourceInfo));
            });
        }

        private async Task<(OperationState, NextStageInput)> CheckCreateKeyVaultStatusAsync(NextStageInput input, IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
                $"{GetType().GetLogMessageBaseName()}_check_create_keyvault",
                async (childLogger) =>
                {
                    var azure = await AzureClientFactory.GetAzureClientAsync(input.AzureResourceInfo.SubscriptionId);
                    var deployment = await azure.Deployments.GetByResourceGroupAsync(input.AzureResourceInfo.ResourceGroup, input.TrackingId);

                    var operationState = DeploymentUtils.ParseProvisioningState(deployment.ProvisioningState);
                    if (operationState == OperationState.Failed)
                    {
                        var errorDetails = await DeploymentUtils.ExtractDeploymentErrors(deployment);
                        throw new KeyVaultException(errorDetails);
                    }

                    return (operationState, new NextStageInput(input.TrackingId, input.AzureResourceInfo));
                },
                (ex, childLogger) =>
                {
                    if (!(ex is KeyVaultException) && input.RetryAttempt < 5)
                    {
                        return Task.FromResult((OperationState.InProgress, new NextStageInput(input.TrackingId, input.AzureResourceInfo, input.RetryAttempt + 1)));
                    }

                    throw ex;
                });
        }

        /// <summary>
        /// Vault name must be between 3 and 24 characters in length.
        /// Vault name must only contain alphanumeric characters and dashes and cannot start with a number.
        /// The name must begin with a letter, end with a letter or digit, and not contain consecutive hyphens.
        /// </summary>
        /// <returns>A valid name for keyvault.</returns>
        private string GenerateKeyVaultName()
        {
            const string Alphabets = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string AlphaNumeric = Alphabets + "0123456789";
            const string AlphaNumericAndHyphen = AlphaNumeric + "-";

            var vaultName = new char[24];
            var random = new CryptoRandom();

            // First character must be a letter.
            var randomCharacterIndex = random.Next(Alphabets.Length);
            vaultName[0] = Alphabets[randomCharacterIndex];

            // Middle characters can be alpha-numberic and hyphens, but cannot contain consecutive hyphens.
            for (int i = 1; i < vaultName.Length - 1; i++)
            {
                var characterSet = AlphaNumericAndHyphen;

                // Prevent concecutive hyphens.
                if (vaultName[i - 1] == '-')
                {
                    characterSet = AlphaNumeric;
                }

                randomCharacterIndex = random.Next(characterSet.Length);
                vaultName[i] = characterSet[randomCharacterIndex];
            }

            // Last character should be aplha-numeric (no hyphen).
            randomCharacterIndex = random.Next(AlphaNumeric.Length);
            vaultName[vaultName.Length - 1] = AlphaNumeric[randomCharacterIndex];

            return new string(vaultName);
        }

        private string GetKeyVaultTemplate()
        {
            const string resourceName = "keyvault_deployment.json";
            var fullyQualifiedResourceName = GetFullyQualifiedResourceName(resourceName);
            return CommonUtils.GetEmbeddedResourceContent(fullyQualifiedResourceName);
        }

        private string GetFullyQualifiedResourceName(string resourceName)
        {
            var namespaceString = typeof(KeyVaultProvider).Namespace;
            return $"{namespaceString}.Templates.{resourceName}";
        }
    }
}
