// <copyright file="Startup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.VsSaaS.AspNetCore.Authentication.JwtBearer;
using Microsoft.VsSaaS.AspNetCore.Hosting;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Common.Warmup;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.TokenService.Authentication;
using Microsoft.VsSaaS.Services.TokenService.Settings;
using Microsoft.VsSaaS.Tokens;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.TokenService
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

        /// <summary>
        /// This method gets called by the runtime.
        /// Use this method to add services to the container.
        /// </summary>
        /// <param name="services">The services collection that should be configured.</param>
        public virtual void ConfigureServices(IServiceCollection services)
        {
            // Frameworks
            services
                .AddControllers(options =>
                {
                    options.AllowEmptyInputInBodyModelBinding = true;
                    options.ModelMetadataDetailsProviders.Add(new ExcludeBindingMetadataProvider(typeof(IDiagnosticsLogger)));
                })
                .AddNewtonsoftJson(x =>
                {
                    x.SerializerSettings.DefaultValueHandling = DefaultValueHandling.Ignore;
                    x.AllowInputFormatterExceptionMessages = HostingEnvironment.IsDevelopment();
                });

            // Configuration
            var appSettings = ConfigureAppSettings(services);
            var tokenServiceAppSettings = appSettings.TokenService;
            services.AddSingleton(tokenServiceAppSettings);

            if (HostingEnvironment.IsDevelopment())
            {
                // Enable PII data in logs for Dev
                IdentityModelEventSource.ShowPII = true;
            }

            // TODO: Switch to separate SP.
            ////AppSettings.ApplicationServicePrincipal = tokenServiceAppSettings.ServicePrincipal;

            // Add front-end/back-end/port-forwarding common services -- secrets, service principal, control-plane resources.
            ConfigureCommonServices(services, out var loggingBaseValues);

            services.AddVsSaaSHosting(
                HostingEnvironment,
                loggingBaseValues,
                loggerFactory: null,
                registerHeartbeatLoggerService: true,
                (keyVaultSecretOptions) =>
                {
                    var servicePrincipal = ApplicationServicesProvider.GetRequiredService<IServicePrincipal>();
                    keyVaultSecretOptions.ServicePrincipalClientId = servicePrincipal.ClientId;
                    keyVaultSecretOptions.GetServicePrincipalClientSecretAsyncCallback = servicePrincipal.GetClientSecretAsync;
                });

            this.ConfigureAuthentication(services);
            this.ConfigureTokenHandling(services);

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
            });
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">The application builder used to set up the pipeline.</param>
        /// <param name="env">The hosting environment for the server.</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var isProduction = env.IsProduction();

            // Frameworks
            app.UseStaticFiles();
            app.UseRouting();

            // Use VS SaaS middleware.
            app.UseVsSaaS(!isProduction);

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

            Warmup(app);
        }

        /// <inheritdoc/>
        protected override string GetSettingsRelativePath()
        {
            var dirChar = Path.DirectorySeparatorChar;
            var settingsRelativePath = IsRunningInAzure() ? string.Empty : Path.GetFullPath(
                Path.Combine(HostingEnvironment.ContentRootPath, "..", "..", "..", $"Settings{dirChar}"));
            return settingsRelativePath;
        }

        /// <summary>
        /// Configures authentication. Separated so it can be overridden for testing.
        /// </summary>
        /// <param name="services">Services collection to be configured.</param>
        protected virtual void ConfigureAuthentication(IServiceCollection services)
        {
            services.AddAuthentication(JwtBearerUtility.AadAuthenticationScheme)
                .AddJwtBearerAuthentication2(
                    JwtBearerUtility.AadAuthenticationScheme,
                    JwtBearerUtility.ConfigureAadOptions);
        }

        /// <summary>
        /// Configures token handling. Separated so it can be overridden for testing.
        /// </summary>
        /// <param name="services">Services collection to be configured.</param>
        protected virtual void ConfigureTokenHandling(IServiceCollection services)
        {
            var tokenReader = new JwtReader();
            services.AddSingleton<IJwtReader>(tokenReader);

            var tokenWriter = new JwtWriter();
            tokenWriter.UnencryptedClaims.Add(JwtRegisteredClaimNames.Exp);
            services.AddSingleton<IJwtWriter>(tokenWriter);

            services.AddSingleton<IAsyncWarmup, TokenHandlingWarmup>();
        }
    }
}
