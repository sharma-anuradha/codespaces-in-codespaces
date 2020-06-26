// <copyright file="SecretManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider
{
    /// <inheritdoc />
    [LoggingBaseName(LoggingBaseName)]
    public class SecretManager : ISecretManager
    {
        private const string LoggingBaseName = "secret_manager";

        /// <summary>
        /// Initializes a new instance of the <see cref="SecretManager"/> class.
        /// </summary>
        /// <param name="resourceRepository">The resource repository.</param>
        /// <param name="mapper">Auto mapper.</param>
        /// <param name="azureSubscriptionCatalog">The azure subscription catalog.</param>
        public SecretManager(
            IResourceRepository resourceRepository,
            IMapper mapper,
            IAzureSubscriptionCatalog azureSubscriptionCatalog)
        {
            ResourceRepository = resourceRepository;
            Mapper = mapper;
            AzureSubscriptionCatalog = azureSubscriptionCatalog;
        }

        private IResourceRepository ResourceRepository { get; }

        private IMapper Mapper { get; }

        private IAzureSubscriptionCatalog AzureSubscriptionCatalog { get; }

        /// <inheritdoc/>
        public async Task<UserSecretResult> CreateSecretAsync(
            Guid resourceId,
            CreateSecretInput createSecretInput,
            IDiagnosticsLogger logger)
        {
            return await logger.RetryOperationScopeAsync($"{LoggingBaseName}_create_secret", async childLogger =>
            {
                Requires.NotEmpty(resourceId, nameof(resourceId));
                Requires.NotNull(logger, nameof(logger));
                Requires.NotNull(createSecretInput, nameof(createSecretInput));
                Requires.NotNullOrEmpty(createSecretInput.SecretName, nameof(createSecretInput.SecretName));
                Requires.NotNullOrEmpty(createSecretInput.Value, nameof(createSecretInput.Value));

                var keyVaultResource = await GetKeyVaultResourceAsync(resourceId, logger.NewChildLogger());
                var secret = Mapper.Map<UserSecret>(createSecretInput);
                secret.Id = Guid.NewGuid();

                var keyVaultSecretsProvider = GetKeyVaultSecretsProvider(keyVaultResource);
                await keyVaultSecretsProvider.CreateOrUpdateSecretAsync(secret.Id.ToString(), createSecretInput.Value, logger.NewChildLogger());

                if (keyVaultResource.UserSecrets == null)
                {
                    keyVaultResource.UserSecrets = new List<UserSecret>();
                }

                keyVaultResource.UserSecrets.Add(secret);
                secret.LastModified = DateTime.UtcNow;
                await ResourceRepository.UpdateAsync(keyVaultResource, logger.NewChildLogger());

                return Mapper.Map<UserSecretResult>(secret);
            });
        }

        /// <inheritdoc/>
        public async Task DeleteSecretAsync(
            Guid resourceId,
            Guid secretId,
            IDiagnosticsLogger logger)
        {
            await logger.RetryOperationScopeAsync($"{LoggingBaseName}_delete_secret", async childLogger =>
            {
                Requires.NotEmpty(resourceId, nameof(resourceId));
                Requires.NotEmpty(secretId, nameof(secretId));

                var keyVaultResource = await GetKeyVaultResourceAsync(resourceId, childLogger.NewChildLogger());

                try
                {
                    var secret = GetSecretFromKeyVaultResource(secretId, keyVaultResource);
                    keyVaultResource.UserSecrets.Remove(secret);
                    await DeleteSecretFromAzureKeyVaultAsync(keyVaultResource, secret.Id.ToString(), childLogger);
                    secret.LastModified = DateTime.UtcNow;
                    await ResourceRepository.UpdateAsync(keyVaultResource, childLogger.NewChildLogger());
                }
                catch (NotFoundException)
                {
                    // Success if the secret is already deleted (not exists)
                    return;
                }
            });
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ResourceSecrets>> GetSecretsAsync(
            IEnumerable<Guid> resourceIds,
            IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync($"{LoggingBaseName}_get_secrets", async childLogger =>
            {
                var resourceSecretsList = new List<ResourceSecrets>(resourceIds.Count());
                ValidateGuids(resourceIds, nameof(resourceIds));
                foreach (var resourceId in resourceIds)
                {
                    var keyVaultResource = await GetKeyVaultResourceAsync(resourceId, logger.NewChildLogger());
                    var resourceSecrets = new ResourceSecrets
                    {
                        ResourceId = Guid.Parse(keyVaultResource.Id),
                        UserSecrets = keyVaultResource.UserSecrets,
                    };
                    resourceSecretsList.Add(resourceSecrets);
                }

                return resourceSecretsList;
            });
        }

        /// <inheritdoc/>
        public async Task<UserSecretResult> UpdateSecretAsync(
            Guid resourceId,
            Guid secretId,
            UpdateSecretInput updateSecretInput,
            IDiagnosticsLogger logger)
        {
            return await logger.RetryOperationScopeAsync($"{LoggingBaseName}_update_secret", async childLogger =>
            {
                Requires.NotEmpty(resourceId, nameof(resourceId));
                Requires.NotEmpty(secretId, nameof(secretId));
                Requires.NotNull(updateSecretInput, nameof(updateSecretInput));

                var isSecretChanged = false;
                var keyVaultResource = await GetKeyVaultResourceAsync(resourceId, childLogger.NewChildLogger());
                var secret = GetSecretFromKeyVaultResource(secretId, keyVaultResource);

                // Update secret name if needed
                if (!string.IsNullOrEmpty(updateSecretInput.SecretName))
                {
                    secret.SecretName = updateSecretInput.SecretName;
                    isSecretChanged = true;
                }

                // Update secret notes if needed
                if (!string.IsNullOrEmpty(updateSecretInput.Notes))
                {
                    secret.Notes = updateSecretInput.Notes;
                    isSecretChanged = true;
                }

                // Update secret filters if needed
                if (updateSecretInput.Filters != null)
                {
                    secret.Filters = updateSecretInput.Filters;
                    isSecretChanged = true;
                }

                // Update secret value in azure key vault if needed
                if (!string.IsNullOrEmpty(updateSecretInput.Value))
                {
                    var keyVaultSecretsProvider = GetKeyVaultSecretsProvider(keyVaultResource);
                    await keyVaultSecretsProvider.CreateOrUpdateSecretAsync(secret.Id.ToString(), updateSecretInput.Value, childLogger.NewChildLogger());
                    isSecretChanged = true;
                }

                // Update resource record in cosmos db if any of it's properties are modified
                if (isSecretChanged)
                {
                    secret.LastModified = DateTime.UtcNow;
                    await ResourceRepository.UpdateAsync(keyVaultResource, childLogger.NewChildLogger());
                }

                return Mapper.Map<UserSecretResult>(secret);
            });
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<UserSecretData>> GetApplicableSecretsAndValuesAsync(
            FilterSecretsInput filterSecretsInput,
            IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync($"{LoggingBaseName}_get_applicable_secrets", async (childLogger) =>
            {
                Requires.NotNull(filterSecretsInput, nameof(filterSecretsInput));
                Requires.NotNullOrEmpty(
                    filterSecretsInput.PrioritizedSecretStoreResources,
                    nameof(filterSecretsInput.PrioritizedSecretStoreResources));

                // Sort resources by priority, such that the secrets from a higher priority keyvault wins when there is a conflict/tie.
                var sortedResourceIds = filterSecretsInput.PrioritizedSecretStoreResources
                                                                   .OrderBy(resource => resource.Priority)
                                                                   .Select(resource => resource.ResourceId);

                var allApplicableSecrets = new List<(ResourceRecord keyVaultResource, IEnumerable<UserSecret> filteredSecrets)>();
                if (sortedResourceIds.Any())
                {
                    foreach (var resourceId in sortedResourceIds)
                    {
                        var keyVaultResource = await GetKeyVaultResourceAsync(resourceId, logger);
                        var secrets = keyVaultResource.UserSecrets;
                        if (secrets != null && secrets.Any())
                        {
                            var applicableSecrets = SecretFilterUtil.ComputeApplicableSecrets(
                                secrets,
                                filterSecretsInput.FilterData);
                            allApplicableSecrets.Add((keyVaultResource, applicableSecrets));
                        }
                    }
                }

                // Fetch secret value from keyvault and consolidate applicable secrets.
                var userSecretDataCollection = new HashSet<UserSecretData>();
                foreach (var applicableKeyVaultSecrets in allApplicableSecrets)
                {
                    // Secrets from higher priotity keyvault wins, if there is a name conflict (as it gets into the hashset first)
                    var data = await GetUserSecretDataWithValuesAsync(
                        applicableKeyVaultSecrets.keyVaultResource,
                        applicableKeyVaultSecrets.filteredSecrets,
                        logger);
                    userSecretDataCollection.UnionWith(data);
                }

                return userSecretDataCollection;
            });
        }

        private static UserSecret GetSecretFromKeyVaultResource(Guid secretId, ResourceRecord keyVaultResource)
        {
            var secret = keyVaultResource.UserSecrets?.SingleOrDefault(x => x.Id == secretId);

            if (secret == null)
            {
                throw new NotFoundException("No secrets found with id '{}' not found.");
            }

            return secret;
        }

        /// <summary>
        /// For the given secrets, get a corresponding collection of UserSecretData objects that has secret type, name and value.
        /// </summary>
        private async Task<IEnumerable<UserSecretData>> GetUserSecretDataWithValuesAsync(
            ResourceRecord keyVaultResource,
            IEnumerable<UserSecret> applicableSecrets,
            IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync($"{LoggingBaseName}", async (childLogger) =>
            {
                childLogger.AddBaseValue("ResourceId", keyVaultResource.Id);
                var secretDataCollection = new HashSet<UserSecretData>();

                var keyVaultSecretsProvider = GetKeyVaultSecretsProvider(keyVaultResource);
                foreach (var secret in applicableSecrets)
                {
                    var maxRetries = 2;
                    await Retry.DoWithCountAsync(maxRetries, async (int attemptNumber) =>
                    {
                        try
                        {
                            var secretValue = await keyVaultSecretsProvider.GetSecretAsync(secret.Id.ToString(), childLogger.NewChildLogger());
                            var userSecretData = new UserSecretData
                            {
                                Type = secret.Type,
                                Name = secret.SecretName,
                                Value = secretValue,
                            };
                            secretDataCollection.Add(userSecretData);
                            return (true, null);
                        }
                        catch (Exception e)
                        {
                            // Azure exceptions while reading secret are not terminal, hence silently given up after max retries.
                            // Exception is already logged inside KeyVaultSecretsProvider.GetSecretAsync method
                            return (false, e);
                        }
                    });
                }

                return secretDataCollection;
            });
        }

        private KeyVaultSecretsProvider GetKeyVaultSecretsProvider(ResourceRecord keyVaultResource)
        {
            var subscriptionId = keyVaultResource.AzureResourceInfo.SubscriptionId;
            var keyVaultName = keyVaultResource.AzureResourceInfo.Name;
            var servicePrincipal = GetServicePrincipalForSubscription(subscriptionId);

            return new KeyVaultSecretsProvider(servicePrincipal, keyVaultName);
        }

        private IServicePrincipal GetServicePrincipalForSubscription(Guid azureSubscriptionId)
        {
            var subscriptionId = azureSubscriptionId.ToString();
            var azureSub = AzureSubscriptionCatalog
                    .AzureSubscriptionsIncludingInfrastructure()
                    .Single(sub => sub.SubscriptionId == subscriptionId && sub.Enabled);
            return azureSub.ServicePrincipal;
        }

        private async Task<ResourceRecord> GetKeyVaultResourceAsync(Guid resourceId, IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("ResourceId", resourceId);

            var keyVaultResource = await ResourceRepository.GetAsync(resourceId.ToString("D"), logger);
            if (keyVaultResource?.Type != ResourceType.KeyVault)
            {
                throw new NotFoundException($"No key vaults found with resource id '{resourceId}'");
            }

            return keyVaultResource;
        }

        private async Task DeleteSecretFromAzureKeyVaultAsync(ResourceRecord keyVaultResource, string secretName, IDiagnosticsLogger logger)
        {
            var keyVaultSecretsProvider = GetKeyVaultSecretsProvider(keyVaultResource);
            await logger.RetryOperationScopeAsync(
                $"{LoggingBaseName}_delete secret_from_azure_keyvault",
                async (childLogger) =>
                {
                    await keyVaultSecretsProvider.DeleteSecretAsync(secretName, childLogger);
                },
                swallowException: true);
        }

        private void ValidateGuids(IEnumerable<Guid> guids, string parameterName)
        {
            Requires.NotNullOrEmpty(guids, parameterName);
            foreach (var guid in guids)
            {
                Requires.NotEmpty(guid, parameterName);
            }
        }
    }
}
