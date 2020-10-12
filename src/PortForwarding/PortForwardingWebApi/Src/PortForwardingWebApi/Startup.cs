// <copyright file="Startup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.VsSaaS.AspNetCore.Hosting;
using Microsoft.VsSaaS.AspNetCore.Http;
using Microsoft.VsSaaS.Caching;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.CodespacesApiClient;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.Common.Clients;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.Common.Routing;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Connections;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Mappings;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Middleware;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile.Http;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi
{
    /// <summary>
    /// Configures the ASP.NET Core pipeline for HTTP requests.
    /// </summary>
    public class Startup : CommonStartupBase<AppSettings>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="hostingEnvironment">The hosting environment for the server.</param>
        public Startup(IWebHostEnvironment hostingEnvironment)
            : base(hostingEnvironment, ServiceConstants.ServiceName)
        {
        }

        private PortForwardingHostUtils PortForwardingHostUtils { get; set; } = default!;

        private PortForwardingRoutingHelper PortForwardingRoutingHelper { get; set; } = default!;

        /// <summary>
        /// This method gets called by the runtime.
        /// Use this method to add services to the container.
        /// </summary>
        /// <param name="services">The services collection that should be configured.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddControllers(options =>
                {
                    options.ModelMetadataDetailsProviders.Add(new ExcludeBindingMetadataProvider(typeof(IDiagnosticsLogger)));
                })
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                });

            // Configuration
            var appSettings = ConfigureAppSettings(services);
            var portForwardingSettings = appSettings.PortForwarding;
            services.AddSingleton(portForwardingSettings);

            // Add front-end/back-end/port-forwarding common services -- secrets, service principal, control-plane resources.
            ConfigureCommonServices(services, AppSettings, null, out var loggingBaseValues);

            services.AddCors(options =>
            {
                var vssaasHeaders = new string[]
                {
                    HttpConstants.CorrelationIdHeader,
                    HttpConstants.RequestIdHeader,
                };

                var currentOrigins = ControlPlaneAzureResourceAccessor.GetStampOrigins();

                // GitHub endpoints that should have access to all VSO service instances (including prod)
                currentOrigins.Add("https://github.com");

                // local dev
                currentOrigins.Add("https://github.localhost");
                currentOrigins.Add("http://github.localhost");
                currentOrigins.Add("https://*.github.localhost");
                currentOrigins.Add("http://*.github.localhost");
                currentOrigins.Add("https://garage.github.com");
                currentOrigins.Add("https://*.review-lab.github.com");

                // github.dev
                currentOrigins.Add("https://*.github.dev");
                currentOrigins.Add("https://*.ppe.github.dev");
                currentOrigins.Add("https://*.dev.github.dev");
                currentOrigins.Add("https://*.local.github.dev");

                options.AddDefaultPolicy(
                    builder => builder
                        .WithOrigins(currentOrigins.ToArray())
                        .AllowAnyHeader()
                        .WithExposedHeaders(vssaasHeaders)
                        .AllowAnyMethod()
                        .SetIsOriginAllowedToAllowWildcardSubdomains());
            });

            // Adding developer personal stamp settings and resource name builder.
            var developerPersonalStampSettings = new DeveloperPersonalStampSettings(AppSettings.DeveloperPersonalStamp, AppSettings.DeveloperAlias, AppSettings.DeveloperKusto);
            services.AddSingleton(developerPersonalStampSettings);
            services.AddSingleton<IResourceNameBuilder, ResourceNameBuilder>();

            services.AddServiceBusClientProviders();

            services.AddSingleton<IManagedCache, InMemoryManagedCache>();
            services.AddSingleton<ISystemCatalog, NullSystemCatalog>();

            PortForwardingHostUtils = new PortForwardingHostUtils(portForwardingSettings.HostsConfigs);
            services.AddSingleton(PortForwardingHostUtils);
            PortForwardingRoutingHelper = new PortForwardingRoutingHelper(PortForwardingHostUtils);

            if (IsRunningInAzure() && (
                portForwardingSettings.UseMockKubernetesMappingClientInDevelopment ||
                portForwardingSettings.DisableBackgroundTasksForLocalDevelopment))
            {
                throw new InvalidOperationException("Cannot use mocks, fakes, or disable background tasks outside of local development.");
            }

            if (!portForwardingSettings.DisableBackgroundTasksForLocalDevelopment)
            {
                services.AddEstablishedConnectionsWorker();
            }

            services
                .AddUserProfile(
                    options =>
                    {
                        options.BaseAddress = ValidationUtil.IsRequired(portForwardingSettings.VSLiveShareApiEndpoint, nameof(portForwardingSettings.VSLiveShareApiEndpoint));
                    });

            services.AddTransient<ForwardingBearerAuthMessageHandler>();
            services.AddHttpCodespacesApiClient((options) =>
            {
                options.BaseAddress = $"https://{appSettings.ControlPlaneSettings.DnsHostName}";
                options.ServiceName = ServiceName;
                options.Version = typeof(Startup).Assembly.GetName().Version!.ToString();
            }).AddHttpMessageHandler<ForwardingBearerAuthMessageHandler>();

            services.AddAgentMappingClient(portForwardingSettings, IsRunningInAzure());
            services.AddSingleton<IConnectionEstablishedMessageHandler, ConnectionEstablishedMessageHandler>();
            services.AddSingleton<IValidatedPrincipalIdentityHandler, ValidatedPrincipalIdentityHandler>();

            // Add the certificate settings.
            services.AddSingleton(appSettings.AuthenticationSettings);

            services.AddCertificateCredentialCacheFactory();

            // VS SaaS services with first party app JWT authentication.
            services.AddVsSaaSHostingWithJwtBearerAuthentication2(
                HostingEnvironment,
                loggingBaseValues,
                JwtBearerUtility.ConfigureAadOptions,
                keyVaultSecretOptions =>
                {
                    var servicePrincipal = ApplicationServicesProvider.GetRequiredService<IServicePrincipal>();
                    keyVaultSecretOptions.ServicePrincipalClientId = servicePrincipal.ClientId;
                    keyVaultSecretOptions.GetServicePrincipalClientSecretAsyncCallback = servicePrincipal.GetClientSecretAsync;
                },
                null,
                true,
                defaultAuthenticationScheme: null, // This auth scheme will run for all requests regardless of if the Controller specifies it
                jwtBearerAuthenticationScheme: JwtBearerUtility.AadAuthenticationScheme); // This auth scheme gets configured with JwtBearerUtility.ConfigureAadOptions above

            // Add user authentication using VSO (Cascade) tokens.
            services.AddAuthentication().AddVsoJwtBearerAuthentication();

            // OpenAPI/swagger
            services.AddSwaggerGen(x =>
            {
                x.SwaggerDoc(ServiceConstants.CurrentApiVersion, new OpenApiInfo()
                {
                    Title = ServiceConstants.ServiceName,
                    Description = ServiceConstants.ServiceDescription,
                    Version = ServiceConstants.CurrentApiVersion,
                });

                x.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "JWT auth token for the user. Example: eyJ2...",
                });
                x.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme()
                        {
                            Reference = new OpenApiReference()
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer",
                            },
                        },
                        new string[0]
                    },
                });

                x.SchemaFilter<EnumSchemaFilter>();
            });
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">The application builder used to set up the pipeline.</param>
        /// <param name="env">The hosting environment for the server.</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            ConfigureAppCommon(app);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.Use(async (httpContext, next) =>
            {
                if (httpContext.Request.Headers.TryGetValue("X-Request-ID", out var nginxRequestId))
                {
                    var requestId = nginxRequestId.SingleOrDefault();
                    if (requestId != default)
                    {
                        httpContext.TrySetRequestId(requestId);
                    }
                }

                await next();
            });

            app.Use(async (httpContext, next) =>
            {
                if (!httpContext.Request.Headers.TryGetValue(HttpConstants.CorrelationIdHeader, out var correlationIdHeader) ||
                    string.IsNullOrEmpty(correlationIdHeader))
                {
                    if (httpContext.Request.Cookies.TryGetValue("codespaces_correlation_id", out var cookieCorrelationId) &&
                        !string.IsNullOrWhiteSpace(cookieCorrelationId))
                    {
                        // VsSaaS SDK middleware overrides correlation id by this header or request id.
                        httpContext.Request.Headers.Add(HttpConstants.CorrelationIdHeader, cookieCorrelationId);
                    }
                }

                await next();
            });

            app.MapWhen(Not<HttpContext>(PortForwardingRoutingHelper.IsPortForwardingRequest), HandleInternalRequests);
            app.MapWhen(PortForwardingRoutingHelper.IsPortForwardingRequest, HandleUserRequests);

            Warmup(app);
        }

        private Func<T, bool> Not<T>(Func<T, bool> predicate)
        {
            return (arg) => !predicate(arg);
        }

        private void HandleUserRequests(IApplicationBuilder app)
        {
            // Use VS SaaS middleware.
            app.UseVsSaaS(!HostingEnvironment.IsProduction());

            app.UseConnectionCreation();
        }

        private void HandleInternalRequests(IApplicationBuilder app)
        {
            app.UseCors();

            app.UseRouting();

            // Use VS SaaS middleware.
            app.UseVsSaaS(!HostingEnvironment.IsProduction());

            app.UseEndpoints(x =>
            {
                x.MapControllers();
            });

            // Swagger/OpenAPI
            app.UseSwagger(x =>
            {
                x.RouteTemplate = "api/{documentName}/swagger";
                x.PreSerializeFilters.Add((swaggerDoc, request) =>
                {
                    var scheme = request.Host.Host == "localhost" ? "http" : "https";
                    var host = request.Host.Value;

                    swaggerDoc.Servers.Add(new OpenApiServer()
                    {
                        Url = $"{scheme}://{host}",
                    });
                });
            });

            app.UseSwaggerUI(c =>
            {
                // UI should be visible from /api/swagger endpoint. Note that leading slash
                // is not present on the RoutePrefix below. This is intentional, because it
                // doesn't work correctly if you add it. I think this is a bug in the Swashbuckle
                // Swagger UI implementation.
                c.RoutePrefix = "api/swagger";

                // The swagger json document is served up from /api/v1/swagger.
                c.SwaggerEndpoint($"/api/{ServiceConstants.CurrentApiVersion}/swagger", ServiceConstants.EndpointName);
                c.DisplayRequestDuration();
            });
        }

        private class EnumSchemaFilter : ISchemaFilter
        {
            public void Apply(OpenApiSchema model, SchemaFilterContext context)
            {
                if (context.Type.IsEnum)
                {
                    model.Enum.Clear();
                    foreach (var value in Enum.GetValues(context.Type))
                    {
                        var displayString = $"{(int)value!} ({value.ToString()})";
                        model.Enum.Add(new OpenApiString(displayString));
                    }
                }
            }
        }
    }
}
