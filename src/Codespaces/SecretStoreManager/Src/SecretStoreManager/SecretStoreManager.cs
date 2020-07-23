// <copyright file="SecretStoreManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.SecretManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceAllocation;
using Microsoft.VsSaaS.Services.CloudEnvironments.SecretStoreManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using SecretScope = Microsoft.VsSaaS.Services.CloudEnvironments.SecretStoreManager.Contracts.SecretScope;
using SecretType = Microsoft.VsSaaS.Services.CloudEnvironments.SecretStoreManager.Contracts.SecretType;
using SecretTypeHttpContract = Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.SecretManager.SecretType;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SecretStoreManager
{
    /// <summary>
    /// Secret store manager.
    /// </summary>
    [LoggingBaseName(LoggingBaseName)]
    public class SecretStoreManager : ISecretStoreManager
    {
        private const string LoggingBaseName = "secret_store_manager";
        private const int MaxEnvironmentVariablesPerSecretStore = 10; // Todo: elpadann - Make it configurable via system configuration, at plan level.

        /// <summary>
        /// Initializes a new instance of the <see cref="SecretStoreManager"/> class.
        /// </summary>
        /// <param name="secretManagerHttpClient">Backend secret manager http client.</param>
        /// <param name="secretStoreRepository">The secret store repository.</param>
        /// <param name="resourceAllocationManager">The resource allocation manager.</param>
        /// <param name="planManager">The plan manager.</param>
        /// <param name="planSkuCatalog">The plan sku catalog.</param>
        /// <param name="currentUserProvider">The current user provider.</param>
        /// <param name="controlPlaneInfo">The control plane info.</param>
        /// <param name="mapper">The Automapper.</param>
        public SecretStoreManager(
            ISecretManagerHttpContract secretManagerHttpClient,
            ISecretStoreRepository secretStoreRepository,
            IResourceAllocationManager resourceAllocationManager,
            IPlanManager planManager,
            IPlanSkuCatalog planSkuCatalog,
            ICurrentUserProvider currentUserProvider,
            IControlPlaneInfo controlPlaneInfo,
            IMapper mapper)
        {
            SecretManagerHttpClient = secretManagerHttpClient;
            SecretStoreRepository = secretStoreRepository;
            ResourceAllocationManager = resourceAllocationManager;
            PlanManager = planManager;
            PlanSkuCatalog = planSkuCatalog;
            CurrentUserProvider = currentUserProvider;
            ControlPlaneInfo = controlPlaneInfo;
            Mapper = mapper;
        }

        private ISecretManagerHttpContract SecretManagerHttpClient { get; }

        private ISecretStoreRepository SecretStoreRepository { get; }

        private IResourceAllocationManager ResourceAllocationManager { get; }

        private IPlanManager PlanManager { get; }

        private IPlanSkuCatalog PlanSkuCatalog { get; }

        private ICurrentUserProvider CurrentUserProvider { get; }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        private IMapper Mapper { get; }

        /// <inheritdoc/>
        public async Task<ScopedSecretResult> CreateSecretAsync(
            string planId,
            ScopedCreateSecretInput scopedCreateSecretInput,
            IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync($"{LoggingBaseName}_create_secret", async (childLogger) =>
            {
                Requires.NotNull(planId, nameof(planId));
                Requires.NotNull(scopedCreateSecretInput, nameof(scopedCreateSecretInput));
                ValidateUserContext();
                AuthorizeSecretScope(scopedCreateSecretInput.Scope);
                var vsoPlan = await GetAuthorizedPlanAsync(planId, childLogger.NewChildLogger());

                var secretStore = await GetSecretStoreAsync(
                    vsoPlan,
                    scopedCreateSecretInput.Scope,
                    createIfNotExists: true,
                    childLogger);

                try
                {
                    bool isSecretQuotaReached = await IsSecretQuotaReached(secretStore, scopedCreateSecretInput.Type, childLogger.NewChildLogger());
                    if (isSecretQuotaReached)
                    {
                        throw new ForbiddenException(
                            (int)MessageCodes.ExceededSecretsQuota,
                            message: $"Quota reached for the secrets type '{scopedCreateSecretInput.Type}'");
                    }

                    var createSecretBody = Mapper.Map<CreateSecretBody>(scopedCreateSecretInput);
                    var secret = await SecretManagerHttpClient.CreateSecretAsync(
                                            secretStore.SecretResource.ResourceId,
                                            createSecretBody,
                                            childLogger);
                    return ScopeSecret(secretStore.Scope, secret);
                }
                catch (Exception e) when (!(e is ForbiddenException))
                {
                    throw new ProcessingFailedException((int)MessageCodes.FailedToCreateSecret, innerException: e);
                }
            });
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ScopedSecretResult>> GetAllSecretsByPlanAsync(
            string planId,
            IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync($"{LoggingBaseName}_get_secrets_by_plan", async (childLogger) =>
            {
                Requires.NotNull(planId, nameof(planId));
                ValidateUserContext();
                var vsoPlan = await GetAuthorizedPlanAsync(planId, childLogger.NewChildLogger());
                var scopedSecrets = new List<ScopedSecretResult>();

                var secretStores = await GetAllSecretStoresByPlanAsync(planId, childLogger);

                // If there are any secret stores in the DB;
                if (secretStores.Any())
                {
                    // Find secrets metatdata records from backend;
                    var resourceSecrets = await FetchResourceSecretsFromBackend(childLogger, secretStores);

                    // Format user secrets from backend into a consolidated list of scoped secrets;
                    foreach (var resourceSecret in resourceSecrets)
                    {
                        var secretStore = secretStores.Single(x => x.SecretResource.ResourceId == resourceSecret.ResourceId);
                        var scopedResourceUserSecrets = ScopeSecrets(resourceSecret.UserSecrets, secretStore.Scope);
                        scopedSecrets.AddRange(scopedResourceUserSecrets);
                    }
                }

                // Return scoped secrets.
                return scopedSecrets;
            });
        }

        /// <inheritdoc/>
        public async Task<ScopedSecretResult> UpdateSecretAsync(
            string planId,
            Guid secretId,
            ScopedUpdateSecretInput scopedUpdateSecretInput,
            IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync($"{LoggingBaseName}_update_secret", async (childLogger) =>
            {
                Requires.NotNull(planId, nameof(planId));
                Requires.NotNull(scopedUpdateSecretInput, nameof(scopedUpdateSecretInput));
                ValidateUserContext();
                AuthorizeSecretScope(scopedUpdateSecretInput.Scope);
                var vsoPlan = await GetAuthorizedPlanAsync(planId, childLogger.NewChildLogger());

                var secretStore = await GetSecretStoreAsync(
                    vsoPlan,
                    scopedUpdateSecretInput.Scope,
                    createIfNotExists: false,
                    childLogger);

                var updateSecretBody = Mapper.Map<UpdateSecretBody>(scopedUpdateSecretInput);
                try
                {
                    var secret = await SecretManagerHttpClient.UpdateSecretAsync(
                        secretStore.SecretResource.ResourceId,
                        secretId,
                        updateSecretBody,
                        childLogger);
                    return ScopeSecret(secretStore.Scope, secret);
                }
                catch (HttpResponseStatusException e) when (e.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new EntityNotFoundException((int)MessageCodes.SecretNotFound, innerException: e);
                }
                catch (Exception e)
                {
                    throw new ProcessingFailedException((int)MessageCodes.FailedToUpdateSecret, innerException: e);
                }
            });
        }

        /// <inheritdoc/>
        public async Task DeleteSecretAsync(
            string planId,
            Guid secretId,
            SecretScope secretScope,
            IDiagnosticsLogger logger)
        {
            await logger.OperationScopeAsync($"{LoggingBaseName}_delete_secret", async (childLogger) =>
            {
                Requires.NotNull(planId, nameof(planId));
                Requires.NotEmpty(secretId, nameof(secretId));
                ValidateUserContext();
                AuthorizeSecretScope(secretScope);
                var vsoPlan = await GetAuthorizedPlanAsync(planId, childLogger.NewChildLogger());

                var secretStore = await GetSecretStoreAsync(
                    vsoPlan,
                    secretScope,
                    createIfNotExists: false,
                    childLogger);

                try
                {
                    await SecretManagerHttpClient.DeleteSecretAsync(
                        secretStore.SecretResource.ResourceId,
                        secretId,
                        childLogger);
                }
                catch (Exception e)
                {
                    throw new ProcessingFailedException((int)MessageCodes.FailedToDeleteSecret, innerException: e);
                }
            });
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<SecretStore>> GetAllSecretStoresByPlanAsync(
            string planId,
            IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync($"{LoggingBaseName}_get_secret_stores_by_plan", async (childLogger) =>
            {
                ValidateUserContext();
                var userId = CurrentUserProvider.CurrentUserIdSet.PreferredUserId;
                return await SecretStoreRepository.GetAllPlanSecretStoresByUserAsync(userId, planId, childLogger);
            });
        }

        private async Task<bool> IsSecretQuotaReached(SecretStore secretStore, SecretType secretType, IDiagnosticsLogger logger)
        {
            var secretContractType = Mapper.Map<SecretTypeHttpContract>(secretType);
            var resourceSecretsResult = await FetchResourceSecretsFromBackend(logger, new List<SecretStore>() { secretStore });
            var count = resourceSecretsResult.Single().UserSecrets?.Count(x => x.Type == secretContractType) ?? 0;

            if (secretType == SecretType.EnvironmentVariable)
            {
                return count >= MaxEnvironmentVariablesPerSecretStore;
            }
            else
            {
                // Validate quota for other types of secrets.
                return false;
            }
        }

        private async Task<IEnumerable<ResourceSecretsResult>> FetchResourceSecretsFromBackend(
        IDiagnosticsLogger logger,
        IEnumerable<SecretStore> secretStores)
        {
            try
            {
                var resourceIds = secretStores.Select(x => x.SecretResource.ResourceId).ToList();
                var resourceSecretResults = await SecretManagerHttpClient.GetSecretsAsync(resourceIds, logger.NewChildLogger());
                int secretStoreCount = secretStores.Count();
                int resourceSecretResultCount = resourceSecretResults.Count();
                if (secretStoreCount != resourceSecretResultCount)
                {
                    throw new ProcessingFailedException($"Count of resources in the backend '{resourceSecretResultCount}' does not match the count of secret stores '{secretStoreCount}'.");
                }

                return resourceSecretResults;
            }
            catch (Exception ex) when (!(ex is ProcessingFailedException))
            {
                throw new ProcessingFailedException("Failed to fetch secrets from the backend.", ex);
            }
        }

        /// <summary>
        /// Scope a list of secrets.
        /// </summary>
        private IEnumerable<ScopedSecretResult> ScopeSecrets(IEnumerable<SecretResult> userSecrets, SecretScope scope)
        {
            foreach (var userSecret in userSecrets)
            {
                yield return ScopeSecret(scope, userSecret);
            }
        }

        /// <summary>
        /// Scope the secret to given secret scope and return it as a <see cref="ScopedSecretResult"/>.
        /// </summary>
        private ScopedSecretResult ScopeSecret(SecretScope scope, SecretResult userSecret)
        {
            var scopedSecret = Mapper.Map<ScopedSecretResult>(userSecret);
            scopedSecret.Scope = scope;
            return scopedSecret;
        }

        private async Task<SecretStore> GetSecretStoreAsync(
            VsoPlan vsoPlan,
            SecretScope secretScope,
            bool createIfNotExists,
            IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync($"{LoggingBaseName}_create_or_get_secret_store", async (childLogger) =>
            {
                string ownerId;
                var planId = vsoPlan.Plan.ResourceId;

                // Secrets with Plan scope are shared across users in that plan.
                if (secretScope == SecretScope.Plan)
                {
                    ownerId = planId;
                }
                else if (secretScope == SecretScope.User)
                {
                    ownerId = CurrentUserProvider.CurrentUserIdSet?.PreferredUserId;
                }
                else
                {
                    throw new ForbiddenException($"Unsupported secret scope {secretScope.ToString()}");
                }

                var keyParameters = (secretScope, ownerId, planId);
                var secretStore = await GetSecretStoreByOwnerAndPlanAsync(keyParameters, logger)
                                  ?? (createIfNotExists ? await CreateSecretsStoreAsync(keyParameters, vsoPlan, logger) : null);

                if (secretStore == null)
                {
                    throw new EntityNotFoundException($"No secret stores exist for the scope {secretScope}");
                }

                logger.AddBaseValue(SecretStoreManagerLoggingConstants.LogValueSecretStoreId, secretStore.Id);

                VerifyIsSecretStoreReady(secretStore);

                return secretStore;
            });
        }

        private async Task<SecretStore> GetSecretStoreByOwnerAndPlanAsync(
            (SecretScope secretScope, string ownerId, string planId) keyParameters,
            IDiagnosticsLogger logger)
        {
            return await SecretStoreRepository.GetSecretStoreByOwnerAndPlanAsync(
                keyParameters.secretScope,
                keyParameters.ownerId,
                keyParameters.planId,
                logger.NewChildLogger());
        }

        private async Task<SecretStore> CreateSecretsStoreAsync(
            (SecretScope secretScope, string ownerId, string planId) keyParameters,
            VsoPlan vsoPlan,
            IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync($"{LoggingBaseName}_create_secret_store", async (childLogger) =>
            {
                var secretsStore = new SecretStore
                {
                    Id = CreateDocumentKey(keyParameters, vsoPlan),
                    PlanId = keyParameters.planId,
                    Scope = keyParameters.secretScope,
                    OwnerId = keyParameters.ownerId,
                    SecretResource = null,
                };

                secretsStore = await SecretStoreRepository.CreateAsync(secretsStore, logger.NewChildLogger());

                try
                {
                    secretsStore.SecretResource = await AllocateSecretResourceAsync(vsoPlan, logger);
                    secretsStore.SecretResource.IsReady = true; // Only allocating from the pool as of now.
                    secretsStore = await SecretStoreRepository.UpdateAsync(secretsStore, logger.NewChildLogger());
                }
                catch (Exception ex)
                {
                    // Deleting the secret store document if the key vault provisioning failed.
                    await SecretStoreRepository.DeleteAsync(secretsStore.Id, secretsStore.PlanId, logger.NewChildLogger());
                    throw new UnavailableException(
                        (int)MessageCodes.FailedToCreateSecretStore,
                        "Secret Resource allocation failed.",
                        ex);
                }

                return secretsStore;
            });
        }

        private string CreateDocumentKey((SecretScope secretScope, string ownerId, string planId) keyParameters, VsoPlan vsoPlan)
        {
            if (keyParameters.secretScope == SecretScope.Plan)
            {
                return $"{vsoPlan.Id}";
            }

            return $"{vsoPlan.Id}_{keyParameters.ownerId}";
        }

        private void VerifyIsSecretStoreReady(SecretStore secretsStore)
        {
            if (secretsStore.SecretResource == null || !secretsStore.SecretResource.IsReady)
            {
                throw new ForbiddenException((int)MessageCodes.NotReady, $"The Secret Store is not ready for use yet.");
            }
        }

        private async Task<ResourceAllocationRecord> AllocateSecretResourceAsync(
            VsoPlan vsoPlan,
            IDiagnosticsLogger logger)
        {
            var resourceType = ResourceType.KeyVault;
            var skuName = GetPlanSkuName(vsoPlan);
            var inputRequest = new AllocateRequestBody
            {
                Type = resourceType,
                SkuName = skuName,
                Location = vsoPlan.Plan.Location,
            };

            var allocationResults = await ResourceAllocationManager.AllocateResourcesAsync(
                default,
                new List<AllocateRequestBody>() { inputRequest },
                logger.NewChildLogger());
            return allocationResults.Single();
        }

        private string GetPlanSkuName(VsoPlan vsoPlan)
        {
            var skuName = vsoPlan.SkuPlan?.Name ?? PlanSkuCatalog.DefaultSkuName;
            if (PlanSkuCatalog.PlanSkus.ContainsKey(skuName))
            {
                return PlanSkuCatalog.PlanSkus[skuName].SkuName;
            }

            throw new ProcessingFailedException($"The SKU '{skuName}' does not exist in the plan sku catalog.");
        }

        private void ValidateUserContext()
        {
            UnauthorizedUtil.IsRequired(CurrentUserProvider.CurrentUserIdSet);
            UnauthorizedUtil.IsRequired(CurrentUserProvider.Identity);
        }

        private void AuthorizeSecretScope(SecretScope scope)
        {
            // If the secret scope is Plan, the user must present a token with plan manager scope.
            // This scope is currently not supported.
            if (scope == SecretScope.Plan)
            {
                throw new ForbiddenException((int)MessageCodes.UnauthorizedScope);
            }
        }

        private async Task<VsoPlan> GetAuthorizedPlanAsync(string planId, IDiagnosticsLogger logger)
        {
            // Validate that the specified plan ID is well-formed.
            ValidationUtil.IsTrue(
                VsoPlanInfo.TryParse(planId, out var plan),
                $"Invalid plan ID: {planId}");

            logger.AddVsoPlanInfo(plan);

            // Validate the plan exists (and lookup the plan details).
            var vsoPlan = await PlanManager.GetAsync(plan, logger);
            ValidationUtil.IsRequired(vsoPlan, $"Plan {planId} not found.");

            logger.FluentAddValue("ResourceLocation", vsoPlan.Plan.Location);

            var isPlanAuthorized = CurrentUserProvider.Identity.IsPlanAuthorized(vsoPlan.Plan.ResourceId);
            if (isPlanAuthorized.HasValue && isPlanAuthorized.Value)
            {
                // Authorized.
            }
            else
            {
                // Users without a scoped access token must be the owner of the plan
                // (if the plan has an owner).
                var currentUserIdSet = CurrentUserProvider.CurrentUserIdSet;
                UnauthorizedUtil.IsTrue(currentUserIdSet.EqualsAny(vsoPlan.UserId));
            }

            // Ensure this is the control plane location that owns the plan.
            ValidateLocation(vsoPlan.Plan.Location);

            return vsoPlan;
        }

        private void ValidateLocation(AzureLocation location)
        {
            // Reroute to correct location if needed
            var owningStamp = ControlPlaneInfo.GetOwningControlPlaneStamp(location);
            if (owningStamp.Location != ControlPlaneInfo.Stamp.Location)
            {
                throw new RedirectToLocationException($"Invalid location", owningStamp.DnsHostName);
            }
        }
    }
}
