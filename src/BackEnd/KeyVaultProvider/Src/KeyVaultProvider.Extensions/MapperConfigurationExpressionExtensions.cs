// <copyright file="MapperConfigurationExpressionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using AutoMapper;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.SecretManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider
{
    /// <summary>
    /// Mapper configuration extension.
    /// </summary>
    public static class MapperConfigurationExpressionExtensions
    {
        /// <summary>
        /// Add mapping.
        /// </summary>
        /// <param name="cfg">Mapper configuration.</param>
        public static void AddSecretManager(this IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<CreateSecretBody, CreateSecretInput>();
            cfg.CreateMap<CreateSecretInput, UserSecret>();
            cfg.CreateMap<UserSecret, UserSecretResult>();
            cfg.CreateMap<UpdateSecretBody, UpdateSecretInput>();
            cfg.CreateMap<UserSecretResult, SecretResult>();
            cfg.CreateMap<ResourceSecrets, ResourceSecretsResult>();
        }
    }
}
