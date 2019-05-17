using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Microsoft.VsSaaS.Azure.Storage.FileShare
{
    public static class FileShareProviderExtensions
    {
        /// <summary>
        /// Adds a <see cref="FileShareProvider"/> to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">The configuration callback.</param>
        /// <returns>Returns <paramref name="services"/>.</returns>
        public static IServiceCollection AddFileShareProvider(
            [ValidatedNotNull] this IServiceCollection services,
            [ValidatedNotNull] Action<FileShareProviderOptions> configureOptions)
        {
            Requires.NotNull(services, nameof(services));
            Requires.NotNull(configureOptions, nameof(configureOptions));

            return services.AddFileShareProvider<FileShareProvider>(configureOptions);
        }

        /// <summary>
        /// Adds a FileShareProvider to the service collection.
        /// </summary>
        /// <typeparam name="TImplementation">The implementation type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">The configuration callback.</param>
        /// <returns>Returns <paramref name="services"/>.</returns>
        public static IServiceCollection AddFileShareProvider<TImplementation>(
            [ValidatedNotNull] this IServiceCollection services,
            [ValidatedNotNull] Action<FileShareProviderOptions> configureOptions)
            where TImplementation : class, IFileShareProvider
        {
            Requires.NotNull(services, nameof(services));
            Requires.NotNull(configureOptions, nameof(configureOptions));

            services.Configure(configureOptions);
            services.TryAddSingleton<IFileShareProvider, TImplementation>();

            return services;
        }
    }
}
