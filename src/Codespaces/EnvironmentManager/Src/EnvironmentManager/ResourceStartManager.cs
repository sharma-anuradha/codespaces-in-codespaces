// <copyright file="ResourceStartManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.SecretManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.SecretStoreManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.SecretStoreManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using SecretFilterType = Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.SecretManager.SecretFilterType;
using SecretScopeModel = Microsoft.VsSaaS.Services.CloudEnvironments.SecretStoreManager.Contracts.SecretScope;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Resource Start Manager.
    /// </summary>
    public class ResourceStartManager : IResourceStartManager
    {
        private static readonly string LogBaseName = "resource_start_manager";

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceStartManager"/> class.
        /// </summary>
        /// <param name="resourceBrokerHttpClient">Target resource broker http client.</param>
        /// <param name="tokenProvider">Target token provider.</param>
        /// <param name="skuCatalog">Target sku catalog.</param>
        /// <param name="secretStoreManager">The secret store manager.</param>
        /// <param name="currentUserProvider">The current user provider.</param>
        /// <param name="superuserIdentity">The superuser Identity.</param>
        public ResourceStartManager(
            IResourceBrokerResourcesExtendedHttpContract resourceBrokerHttpClient,
            ITokenProvider tokenProvider,
            ISkuCatalog skuCatalog,
            ISecretStoreManager secretStoreManager,
            ICurrentUserProvider currentUserProvider,
            VsoSuperuserClaimsIdentity superuserIdentity)
        {
            ResourceBrokerClient = resourceBrokerHttpClient;
            TokenProvider = tokenProvider;
            SkuCatalog = skuCatalog;
            SecretStoreManager = secretStoreManager;
            CurrentUserProvider = currentUserProvider;
            SuperuserIdentity = superuserIdentity;
        }

        private IResourceBrokerResourcesExtendedHttpContract ResourceBrokerClient { get; }

        private ITokenProvider TokenProvider { get; }

        private ISkuCatalog SkuCatalog { get; }

        private ISecretStoreManager SecretStoreManager { get; }

        private ICurrentUserProvider CurrentUserProvider { get; }

        private VsoSuperuserClaimsIdentity SuperuserIdentity { get; }

        /// <inheritdoc/>
        public async Task<bool> StartComputeAsync(
            CloudEnvironment cloudEnvironment,
            Guid computeResourceId,
            Guid? osDiskResourceId,
            Guid? storageResourceId,
            Guid? archiveStorageResourceId,
            CloudEnvironmentOptions cloudEnvironmentOptions,
            StartCloudEnvironmentParameters startCloudEnvironmentParameters,
            IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
                $"{LogBaseName}_start_compute",
                async (childLogger) =>
                {
                    // Base validation
                    Requires.NotNull(startCloudEnvironmentParameters, nameof(startCloudEnvironmentParameters));
                    Requires.NotNullOrEmpty(startCloudEnvironmentParameters.CallbackUriFormat, nameof(startCloudEnvironmentParameters.CallbackUriFormat));
                    Requires.NotNull(startCloudEnvironmentParameters.FrontEndServiceUri, nameof(startCloudEnvironmentParameters.FrontEndServiceUri));

                    var callbackUri = new Uri(string.Format(startCloudEnvironmentParameters.CallbackUriFormat, cloudEnvironment.Id));
                    Requires.Argument(callbackUri.IsAbsoluteUri, nameof(callbackUri), "Must be an absolute URI.");

                    if (!SkuCatalog.CloudEnvironmentSkus.TryGetValue(cloudEnvironment.SkuName, out var sku))
                    {
                        throw new ArgumentException($"Invalid SKU: {cloudEnvironment.SkuName}");
                    }

                    // Geneate token
                    var connectionToken = await TokenProvider.GenerateEnvironmentConnectionTokenAsync(
                        cloudEnvironment, sku, startCloudEnvironmentParameters.UserProfile, logger);

                    // Construct the start-compute environment variables
                    var environmentVariables = EnvironmentVariableGenerator.Generate(
                        cloudEnvironment,
                        startCloudEnvironmentParameters.FrontEndServiceUri,
                        callbackUri,
                        connectionToken,
                        cloudEnvironmentOptions);

                    // Construct the data for secret filtering
                    var filterSecrets = await ConstructFilterSecretsDataAsync(cloudEnvironment, startCloudEnvironmentParameters.CurrentUserIdSet, logger.NewChildLogger());

                    // Setup input requests
                    var resources = new List<StartRequestBody>
                    {
                        new StartRequestBody
                        {
                            ResourceId = computeResourceId,
                            Variables = environmentVariables,
                            FilterSecrets = filterSecrets,
                        },
                    };

                    if (osDiskResourceId.HasValue)
                    {
                        resources.Add(new StartRequestBody
                        {
                            ResourceId = osDiskResourceId.Value,
                        });
                    }

                    if (storageResourceId.HasValue)
                    {
                        resources.Add(new StartRequestBody
                        {
                            ResourceId = storageResourceId.Value,
                        });
                    }

                    if (archiveStorageResourceId.HasValue)
                    {
                        resources.Add(new StartRequestBody
                        {
                            ResourceId = archiveStorageResourceId.Value,
                        });
                    }

                    // Execute start
                    return await ResourceBrokerClient.StartAsync(
                         Guid.Parse(cloudEnvironment.Id),
                         StartRequestAction.StartCompute,
                         resources,
                         logger.NewChildLogger());
                });
        }

        private async Task<FilterSecretsBody> ConstructFilterSecretsDataAsync(CloudEnvironment cloudEnvironment, UserIdSet userIdSet, IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
                $"{LogBaseName}_construct_filter_secrets_data",
                async (childLogger) =>
                {
                    var planId = Requires.NotNull(cloudEnvironment.PlanId, nameof(cloudEnvironment.PlanId));
                    var filterSecretsBody = default(FilterSecretsBody);
                    var secretStores = Enumerable.Empty<SecretStore>();

                    // If the call is coming from the queue, use the superuser's identity
                    if (CurrentUserProvider.Identity is VsoAnonymousClaimsIdentity)
                    {
                        using (CurrentUserProvider.SetScopedIdentity(SuperuserIdentity, userIdSet))
                        {
                            secretStores = await SecretStoreManager.GetAllSecretStoresByPlanAsync(planId, logger);
                        }
                    }
                    else
                    {
                        // Otherwise, use the user's identity
                        secretStores = await SecretStoreManager.GetAllSecretStoresByPlanAsync(planId, logger);
                    }

                    if (secretStores.Any())
                    {
                        var prioritizedSecretStoreResources = new List<PrioritizedSecretStoreResource>();
                        var planScopeSecretStore = secretStores.SingleOrDefault(secretStore => secretStore.Scope == SecretScopeModel.Plan &&
                                                                                               secretStore.SecretResource?.ResourceId != default &&
                                                                                               secretStore.SecretResource?.IsReady == true);
                        var userScopeSecretStore = secretStores.SingleOrDefault(secretStore => secretStore.Scope == SecretScopeModel.User &&
                                                                                               secretStore.SecretResource?.ResourceId != default &&
                                                                                               secretStore.SecretResource?.IsReady == true);

                        if (planScopeSecretStore != default)
                        {
                            prioritizedSecretStoreResources.Add(new PrioritizedSecretStoreResource
                            {
                                Priority = 2,
                                ResourceId = planScopeSecretStore.SecretResource.ResourceId,
                            });
                        }

                        if (userScopeSecretStore != default)
                        {
                            prioritizedSecretStoreResources.Add(new PrioritizedSecretStoreResource
                            {
                                Priority = 1,
                                ResourceId = userScopeSecretStore.SecretResource.ResourceId,
                            });
                        }

                        if (prioritizedSecretStoreResources.Any())
                        {
                            var secretFilterDataCollection = new List<SecretFilterData>();

                            // Add git repo filter data
                            secretFilterDataCollection.Add(new SecretFilterData
                            {
                                Type = SecretFilterType.GitRepo,
                                Data = cloudEnvironment.Seed?.SeedMoniker ?? string.Empty,
                            });

                            // Add codespace name filter data
                            secretFilterDataCollection.Add(new SecretFilterData
                            {
                                Type = SecretFilterType.CodespaceName,
                                Data = cloudEnvironment.FriendlyName,
                            });

                            filterSecretsBody = new FilterSecretsBody
                            {
                                FilterData = secretFilterDataCollection,
                                PrioritizedSecretStoreResources = prioritizedSecretStoreResources,
                            };
                        }
                    }

                    return filterSecretsBody;
                },
                swallowException: true);
        }
    }
}