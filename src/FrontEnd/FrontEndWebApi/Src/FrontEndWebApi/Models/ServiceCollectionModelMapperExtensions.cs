// <copyright file="ServiceCollectionModelMapperExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.SecretManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.SecretStoreManager.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models
{
    /// <summary>
    /// <see cref="IServiceCollection"/> extensions for <see cref="IMapper"/>.
    /// </summary>
    public static class ServiceCollectionModelMapperExtensions
    {
        /// <summary>
        /// Configure and add <see cref="IMapper"/> for environment registration models.
        /// </summary>
        /// <param name="services">The service collection instance.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddModelMapper(this IServiceCollection services)
        {
            services.AddSingleton(serviceProvider =>
            {
                var frontEndAppSettings = serviceProvider.GetService<FrontEndAppSettings>();
                var skuCatalog = serviceProvider.GetService<ISkuCatalog>();
                var config = new MapperConfiguration(cfg =>
                {
                    cfg.CreateMap<CloudEnvironment, CloudEnvironmentResult>()
                        .ForMember(dest => dest.SkuDisplayName, opt => opt.MapFrom(new SkuDisplayNameMapper(skuCatalog)));
                    cfg.CreateMap<ConnectionInfo, ConnectionInfoBody>()
                        .ForMember(dest => dest.ConnectionServiceUri, opt => opt.NullSubstitute(frontEndAppSettings.VSLiveShareApiEndpoint));
                    cfg.CreateMap<CreateCloudEnvironmentBody, CloudEnvironment>();
                    cfg.CreateMap<ConnectionInfoBody, ConnectionInfo>();
                    cfg.CreateMap<SeedInfoBody, SeedInfo>();
                    cfg.CreateMap<GitConfigOptionsBody, GitConfigOptions>();
                    cfg.CreateMap<EnvironmentRegistrationCallbackBody, EnvironmentRegistrationCallbackOptions>();
                    cfg.CreateMap<EnvironmentRegistrationCallbackPayloadBody, EnvironmentRegistrationCallbackPayloadOptions>();
                    cfg.CreateMap<PlanResource, VsoPlan>();
                    cfg.CreateMap<PlanResourceUpdateBody, VsoPlan>();
                    cfg.CreateMap<UpdateCloudEnvironmentBody, CloudEnvironmentUpdate>();
                    cfg.CreateMap<PlanAccessToken, PlanAccessTokenResult>();
                    cfg.CreateMap<ScopedCreateSecretBody, ScopedCreateSecretInput>();
                    cfg.CreateMap<ScopedCreateSecretInput, CreateSecretInput>();
                    cfg.CreateMap<SecretResult, ScopedSecretResult>();
                    cfg.CreateMap<ScopedSecretResult, ScopedSecretResultBody>();
                });
                return config.CreateMapper();
            });
            return services;
        }
    }
}
