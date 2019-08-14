// <copyright file="ServiceCollectionModelMapperExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models
{
    /// <summary>
    /// <see cref="IServiceCollection"/> extensions for <see cref="IMapper"/>.
    /// </summary>
    internal static class ServiceCollectionModelMapperExtensions
    {
        /// <summary>
        /// Configure and add <see cref="IMapper"/> for environment registration models.
        /// </summary>
        /// <param name="services">The service collection instance.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        public static IServiceCollection AddModelMapper(this IServiceCollection services)
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<CloudEnvironment, CloudEnvironmentResult>();
                cfg.CreateMap<CreateCloudEnvironmentBody, CloudEnvironment>();
                cfg.CreateMap<CallbackOptionsBody, CallbackOptions>();
                cfg.CreateMap<CallbackOptions, CallbackOptionsBody>();
                cfg.CreateMap<CallbackPayloadOptionsBody, CallbackPayloadOptions>();
                cfg.CreateMap<CallbackPayloadOptions, CallbackPayloadOptionsBody>();
                cfg.CreateMap<ConnectionInfoBody, ConnectionInfo>();
                cfg.CreateMap<ConnectionInfo, ConnectionInfoBody>();
                cfg.CreateMap<GitConfigOptionsBody, GitConfigOptions>();
                cfg.CreateMap<GitConfigOptions, GitConfigOptionsBody>();
                cfg.CreateMap<SeedInfoBody, SeedInfo>();
                cfg.CreateMap<SeedInfo, SeedInfoBody>();
            });
            var mapper = config.CreateMapper();
            services.AddSingleton(mapper);
            return services;
        }
    }
}
