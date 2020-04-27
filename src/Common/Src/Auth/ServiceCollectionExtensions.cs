// <copyright file="ServiceCollectionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Common.Warmup;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Tokens;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Auth.Extensions
{
    /// <summary>
    /// Extensions methods for <see cref="IServiceCollection"/> related to the Token creation.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a certificate credential cache factory to the collection of services.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <returns>Collection of services along with a certificate credential cache factory.</returns>
        public static IServiceCollection AddCertificateCredentialCacheFactory(
            this IServiceCollection services)
        {
            Requires.NotNull(services, nameof(services));

            return services
                .AddSingleton<IJwtCertificateCredentialsKeyVaultCacheFactory, JwtCertificateCredentialsKeyVaultCacheFactory>();
        }

        /// <summary>
        /// Adds a token writer to the collection of services.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="authSettings">Global authentication settings.</param>
        /// <returns>Collection of services along with token writer.</returns>
        public static IServiceCollection AddTokenProvider(
            this IServiceCollection services,
            AuthenticationSettings authSettings)
        {
            Requires.NotNull(services, nameof(services));
            Requires.NotNull(authSettings, nameof(authSettings));

            if (authSettings.UseTokenService)
            {
                return services
                    .Configure((TokenServiceHttpClientProviderOptions options) =>
                    {
                        options.BaseAddress = ValidationUtil.IsRequired(
                            authSettings.TokenServiceBaseAddress,
                            nameof(authSettings.TokenServiceBaseAddress));
                    })
                    .AddSingleton<IHttpClientProvider<TokenServiceHttpClientProviderOptions>,
                        TokenServiceHttpClientProvider>()
                    .AddSingleton<ITokenProvider, RemoteTokenProvider>();
            }
            else
            {
                var writer = new JwtWriter();
                writer.UnencryptedClaims.Add(JwtRegisteredClaimNames.Exp);

                return services
                    .AddSingleton<IJwtCertificateCredentialsKeyVaultCacheFactory,
                        JwtCertificateCredentialsKeyVaultCacheFactory>()
                    .AddTokenSettingsToJwtWriter(writer, authSettings.VmTokenSettings)
                    .AddTokenSettingsToJwtWriter(writer, authSettings.VsSaaSTokenSettings)
                    .AddTokenSettingsToJwtWriter(writer, authSettings.ConnectionTokenSettings)
                    .AddSingleton<IJwtWriter>(writer)
                    .AddSingleton<ITokenProvider, LocalTokenProvider>();
            }
        }

        /// <summary>
        /// Adds issuer and audience credential caches to the <see cref="IJwtReader"/> and adds the caches to the services warmups.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="reader">JWT reader.</param>
        /// <param name="tokenSettingsSelector">Callback to select the desired token settings.</param>
        /// <returns>Collection of services along with the warmups for the credential caches.</returns>
        public static IServiceCollection AddTokenSettingsToJwtReader(
            this IServiceCollection services,
            IJwtReader reader,
            Func<AuthenticationSettings, TokenSettings> tokenSettingsSelector)
        {
            return services
                .AddSingleton((servicesProvider) =>
                {
                    var tokenSettings = tokenSettingsSelector(servicesProvider.GetRequiredService<AuthenticationSettings>());
                    return AddIssuerToReader(reader, tokenSettings, servicesProvider);
                })
                .AddSingleton((servicesProvider) =>
                {
                    var tokenSettings = tokenSettingsSelector(servicesProvider.GetRequiredService<AuthenticationSettings>());
                    return AddAudienceToReader(reader, tokenSettings, servicesProvider);
                });
        }

        private static IServiceCollection AddTokenSettingsToJwtWriter(
            this IServiceCollection services,
            IJwtWriter writer,
            TokenSettings tokenSettings)
        {
            return services
                .AddSingleton((servicesProvider) => AddIssuerToWriter(writer, tokenSettings, servicesProvider))
                .AddSingleton((servicesProvider) => AddAudienceToWriter(writer, tokenSettings, servicesProvider));
        }

        private static IAsyncWarmup AddIssuerToWriter(IJwtWriter writer, TokenSettings tokenSettings, IServiceProvider serviceProvider)
        {
            var certCacheFactory = serviceProvider.GetRequiredService<IJwtCertificateCredentialsKeyVaultCacheFactory>();

            var issuerCache = certCacheFactory.New(tokenSettings.IssuerCertificateName, keyVaultName: tokenSettings.KeyVaultName);

            writer.AddIssuer(tokenSettings.Issuer, issuerCache);

            return issuerCache;
        }

        private static IAsyncWarmup AddAudienceToWriter(IJwtWriter writer, TokenSettings tokenSettings, IServiceProvider serviceProvider)
        {
            if (string.IsNullOrWhiteSpace(tokenSettings.AudienceCertificateName))
            {
                writer.AddAudience(tokenSettings.Audience);
                return NoOpAsyncWarmup.Instance;
            }
            else
            {
                var certCacheFactory = serviceProvider.GetRequiredService<IJwtCertificateCredentialsKeyVaultCacheFactory>();
                var audienceCache = certCacheFactory.New(tokenSettings.AudienceCertificateName, keyVaultName: tokenSettings.KeyVaultName);
                writer.AddAudience(tokenSettings.Audience, audienceCache.ConvertToPublic());
                return audienceCache;
            }
        }

        private static IAsyncWarmup AddIssuerToReader(IJwtReader reader, TokenSettings tokenSettings, IServiceProvider serviceProvider)
        {
            var certCacheFactory = serviceProvider.GetRequiredService<IJwtCertificateCredentialsKeyVaultCacheFactory>();
            var issuerCache = certCacheFactory.New(tokenSettings.IssuerCertificateName);
            reader.AddIssuer(tokenSettings.Issuer, issuerCache.ConvertToPublic());
            return issuerCache;
        }

        private static IAsyncWarmup AddAudienceToReader(IJwtReader reader, TokenSettings tokenSettings, IServiceProvider serviceProvider)
        {
            if (string.IsNullOrWhiteSpace(tokenSettings.AudienceCertificateName))
            {
                // For unencrypted tokens, add a valid audience with no decryption credentials.
                reader.AddAudience(tokenSettings.Audience);
                return NoOpAsyncWarmup.Instance;
            }
            else
            {
                var certCacheFactory = serviceProvider.GetRequiredService<IJwtCertificateCredentialsKeyVaultCacheFactory>();

                var audienceCache = certCacheFactory.New(tokenSettings.AudienceCertificateName);

                reader.AddAudience(tokenSettings.Audience, audienceCache);
                return audienceCache;
            }
        }

        private class NoOpAsyncWarmup : IAsyncWarmup
        {
            private NoOpAsyncWarmup()
            {
            }

            public static NoOpAsyncWarmup Instance { get; } = new NoOpAsyncWarmup();

            public Task WarmupCompletedAsync()
            {
                return Task.CompletedTask;
            }
        }
    }
}
