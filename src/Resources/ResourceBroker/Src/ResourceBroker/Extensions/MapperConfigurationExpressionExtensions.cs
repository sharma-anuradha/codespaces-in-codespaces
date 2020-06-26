// <copyright file="MapperConfigurationExpressionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using AutoMapper;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.SecretManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions
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
        public static void AddResourceBroker(this IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<AllocateRequestBody, AllocateInput>();
            cfg.CreateMap<ResourceRecord, AllocateResult>();
            cfg.CreateMap<ResourcePool, ResourcePool>();
            cfg.CreateMap<FileShareProviderAssignResult, ShareConnectionInfo>();
            cfg.CreateMap<HeartBeatBody, HeartBeatInput>();
            cfg.CreateMap<HeartBeatInput, ResourceHeartBeatRecord>();
            cfg.CreateMap<StartRequestBody, StartInput>();
            cfg.CreateMap<FilterSecretsBody, FilterSecretsInput>();
        }
    }
}
