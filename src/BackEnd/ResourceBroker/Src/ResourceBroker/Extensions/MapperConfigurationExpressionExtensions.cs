// <copyright file="MapperConfigurationExpressionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using AutoMapper;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions
{
    /// <summary>
    ///
    /// </summary>
    public static class MapperConfigurationExpressionExtensions
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="cfg"></param>
        public static void AddResourceBroker(this IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<ResourceRecord, AllocateResult>();
            cfg.CreateMap<ScalingInput, ResourcePoolDefinition>();
            cfg.CreateMap<FileShareProviderAssignResult, ShareConnectionInfo>();
            cfg.CreateMap<VirtualMachineProviderStartComputeResult, EnvironmentStartResult>();
        }
    }
}
