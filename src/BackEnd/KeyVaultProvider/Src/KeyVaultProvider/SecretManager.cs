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
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Models;
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
        public Task AddOrUpdateSecreFiltersAsync(
            Guid resourceId,
            Guid secretId,
            IDictionary<SecretFilterType, string> secretFilters,
            IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<UserSecretResult> CreateSecretAsync(
            Guid resourceId,
            CreateSecretInput createSecretInput,
            IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync($"{LoggingBaseName}_create_secret", async childLogger =>
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
        public Task DeleteSecretAsync(
            Guid resourceId,
            Guid secretId,
            IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteSecretFilterAsync(
            Guid resourceId,
            Guid secretId,
            SecretFilterType secretFilterType,
            IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ResourceSecrets>> GetSecretsAsync(
            IEnumerable<Guid> resourceIds,
            IDiagnosticsLogger logger)
        {
            var resourceSecretsList = new List<ResourceSecrets>(resourceIds.Count());
            return await logger.OperationScopeAsync($"{LoggingBaseName}_get_secrets", async childLogger =>
            {
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
        public Task<UserSecretResult> UpdateSecretAsync(
            Guid resourceId,
            Guid secretId,
            UpdateSecretInput secret,
            IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
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
            var keyVaultResource = await ResourceRepository.GetAsync(resourceId.ToString("D"), logger);
            if (keyVaultResource?.Type != ResourceType.KeyVault)
            {
                throw new KeyVaultResourceNotFoundException(resourceId);
            }

            return keyVaultResource;
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
