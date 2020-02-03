// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Common.Warmup;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Warmup
{
    /// <summary>
    /// Extensions methods for <see cref="IServiceCollection"/> providing helpers for warmup tasks.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a factory singleton to the collection of services for the specified types along with a warmup callback.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="implementationFactory">Callback returning the instance to add to the services and associated warmup tasks.</param>
        /// <returns>Collection of services along with a certificate credential cache factory.</returns>
        /// <typeparam name="TService">The service type.</typeparam>
        /// <typeparam name="TImplementation">The implementation type.</typeparam>
        public static IServiceCollection AddSingletonWithWarmups<TService, TImplementation>(
            this IServiceCollection services,
            Func<IServiceProvider, (TImplementation, IEnumerable<Task>)> implementationFactory)
            where TImplementation : TService
            where TService : class
        {
            Requires.NotNull(services, nameof(services));
            Requires.NotNull(implementationFactory, nameof(implementationFactory));

            var loader = new Lazy<IServiceProvider, (TImplementation, IEnumerable<Task>)>((serviceProvider) => implementationFactory(serviceProvider));

            return services
                .AddSingleton<TService>((serviceProvider) =>
                {
                    var (impl, _) = loader.GetValue(serviceProvider);
                    return impl;
                })
                .AddSingleton<IAsyncWarmup>((serviceProvider) =>
                {
                    var (_, tasks) = loader.GetValue(serviceProvider);
                    return new TaskSetAsyncWarmup(tasks);
                });
        }

        /// <summary>
        /// Adds a certificate credential cache factory to the collection of services.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="callback">Callback returning the set of warmup tasks.</param>
        /// <returns>Collection of services along with a certificate credential cache factory.</returns>
        public static IServiceCollection AddWarmups(
            this IServiceCollection services,
            Func<IServiceProvider, IEnumerable<Task>> callback)
        {
            Requires.NotNull(services, nameof(services));
            Requires.NotNull(callback, nameof(callback));

            return services
                .AddSingleton<IAsyncWarmup>((serviceProvider) =>
                {
                    return new TaskSetAsyncWarmup(callback(serviceProvider));
                });
        }

        private class Lazy<T1, TVal>
        {
            private readonly Func<T1, TVal> callback;

            private TVal value;
            private bool isLoaded = false;

            public Lazy(Func<T1, TVal> callback)
            {
                this.callback = callback;
            }

            public TVal GetValue(T1 obj)
            {
                if (!this.isLoaded)
                {
                    this.isLoaded = true;
                    this.value = this.callback(obj);
                }

                return this.value;
            }
        }

        private class TaskSetAsyncWarmup : IAsyncWarmup
        {
            private IEnumerable<Task> tasks;

            public TaskSetAsyncWarmup(IEnumerable<Task> tasks)
            {
                this.tasks = tasks;
            }

            public Task WarmupCompletedAsync()
            {
                return Task.WhenAll(this.tasks);
            }
        }
    }
}
