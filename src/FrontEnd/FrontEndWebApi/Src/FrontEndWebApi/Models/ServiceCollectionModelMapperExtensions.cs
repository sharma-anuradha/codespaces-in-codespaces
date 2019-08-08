// <copyright file="ServiceCollectionModelMapperExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
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
                cfg.CreateMap<CallbackInput, CallbackOptions>();
                cfg.CreateMap<CallbackPayloadInput, CallbackPayloadOptions>();
                cfg.CreateMap<CloudEnvironment, CloudEnvironmentResult>();
                cfg.CreateMap<CloudEnvironmentInput, CloudEnvironment>();
                cfg.CreateMap<ConnectionInfoInput, ConnectionInfo>();
                cfg.CreateMap<GitConfigInput, GitConfigOptions>();
                cfg.CreateMap<SeedInfoInput, SeedInfo>();
            });
            var mapper = config.CreateMapper();
            services.AddSingleton(mapper);
            return services;
        }
    }
}
