using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsCloudKernel.Services.EnvReg.Models;
using Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore;
using Microsoft.VsCloudKernel.Services.EnvReg.Repositories;
using Microsoft.VsCloudKernel.Services.EnvReg.Repositories.DocumentDb;
using Microsoft.VsCloudKernel.Services.EnvReg.WebApi.Authentication;
using Microsoft.VsCloudKernel.Services.EnvReg.WebApi.Provider;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Azure.Storage.FileShare;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Diagnostics.Health;
using VsClk.EnvReg.Repositories;
using Microsoft.VsCloudKernel.Services.EnvReg.WebApi.Provider;
using VsClk.EnvReg.Repositories.Support.HttpClient;
using VsClk.EnvReg.Repositories.HttpClient;
using Microsoft.VsCloudKernel.Services.EnvReg.WebApi.Authentication;
using Swashbuckle.AspNetCore.SwaggerUI;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Authorization;

namespace Microsoft.VsCloudKernel.Services.EnvReg.WebApi
{
    public class Startup
    {
        public static string ApiName = "Environment registration Api";
        public Startup(IConfiguration configuration, IHostingEnvironment hostEnvironment)
        {
            HostEnvironment = hostEnvironment;
            Configuration = configuration;
        }

        private IHostingEnvironment HostEnvironment { get; }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddDefaultPolicy((builder) =>
                {
                    builder.WithOrigins(
                        // Localhost for Portal development
                        "https://localhost:3000")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            // Frameworks
            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            // Configuration
            var appSettingsConfiguration = Configuration.GetSection("AppSettings");
            var appSettings = appSettingsConfiguration.Get<AppSettings>();
            services.Configure<AppSettings>(appSettingsConfiguration);
            services.AddSingleton(appSettings);

            // Mappers
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<EnvironmentRegistration, EnvironmentRegistrationResult>();
                cfg.CreateMap<EnvironmentRegistrationInput, EnvironmentRegistration>();
                cfg.CreateMap<ConnectionInfoInput, ConnectionInfo>();
                cfg.CreateMap<SeedInfoInput, SeedInfo>();
                cfg.CreateMap<GitConfigInput, GitConfig>();
                cfg.CreateMap<EnvironmentRegistrationCallbackInput, EnvironmentRegistrationCallbackOptions>();
                cfg.CreateMap<EnvironmentRegistrationCallbackPayloadInput, EnvironmentRegistrationCallbackPayloadOptions>();
            });
            var mapper = config.CreateMapper();
            services.AddSingleton(mapper);

            ConfigureDbServices(services, appSettings);

            // Providers
            services.AddSingleton<ICurrentUserProvider, HttpContextCurrentUserProvider>();
            services.AddSingleton<IProfileCache, HttpContextProfileCache>();
            services.AddSingleton<IHttpClientProvider, HttpClientProvider>();

            // Repositories
            services.AddSingleton<IRegistrationManager, RegistrationManager>();
            services.AddSingleton<IProfileRepository, HttpClientProfileRepository>();
            services.AddSingleton<IWorkspaceRepository, HttpClientWorkspaceRepository>();
            services.AddSingleton<IAuthRepository, HttpClientAuthRepository>();
            ConfigureComputeService(services, appSettings);


            // Authentication
            services.AddVsClkCoreAuthenticationServices(HostEnvironment, appSettings);

            // VS SaaS services
            services.AddVsSaaSHosting(HostEnvironment, new LoggingBaseValues
            {
                CommitId = appSettings.GitCommit,
                ServiceName = "envreg",
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info
                {
                    Title = ApiName,
                    Version = "V1",
                    Description = ApiName + " is used to vscode extensions to do environment related operations for cloud environments. <h3><b>Note:</b> Paste the bearer token after the word 'bearer '</h3>",
                });
                c.EnableAnnotations();
                c.OperationFilter<AddAuthorizationHeaderParameterOperationFilter>();
            });

            // pretty print
            services.AddMvc().AddJsonOptions(options => {
                options.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
            });

            services.AddFileShareProvider(options =>
            {
                options.AccountName = appSettings.StorageAccountName;
                options.AccountKey = appSettings.StorageAccountKey;
            });
            services.AddTransient<IStorageManager, StorageManager>();
        }

        private void ConfigureDbServices(IServiceCollection services, AppSettings appSettings)
        {
            // Document DB Repositories
#if DEBUG
            if (appSettings.UseMocksForLocalDevelopment)
            {
                // Use the mock db if we're developing locally
                services.AddSingleton<IEnvironmentRegistrationRepository, MockEnvironmentRegistrationRepository>();
                services.AddSingleton<IStorageRegistrationRepository, MockStorageRegistrationRepository>();

                return;
            }
#endif

            services.AddDocumentDbClientProvider(options =>
            {
                options.HostUrl = appSettings.VsClkEnvRegDbHost;
                options.AuthKey = appSettings.VsClkEnvRegDbKey;
                options.DatabaseId = appSettings.VsClkEnvRegDbId;
                options.PreferredLocation = appSettings.VsClkEnvRegPreferredLocation;
            });
            services.AddDocumentDbCollection<EnvironmentRegistration, IEnvironmentRegistrationRepository, DocumentDbEnvironmentRegistrationRepository>(
                DocumentDbEnvironmentRegistrationRepository.ConfigureOptions);
            services.AddDocumentDbCollection<FileShare, IStorageRegistrationRepository, DocumentDbSotrageRegistrationRepository>(DocumentDbSotrageRegistrationRepository.ConfigureOptions);
        }

        private void ConfigureComputeService(IServiceCollection services, AppSettings appSettings)
        {
#if DEBUG
            if (appSettings.UseMocksForLocalDevelopment)
            {
                services.AddSingleton<IComputeRepository, MockComputeRepository>((x) => new MockComputeRepository(appSettings));
                return;
            }
#endif // DEBUG

            services.AddSingleton<IComputeRepository, HttpClientComputeRepository>();
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            //Use generted swagger.json
            app.UseSwagger();

            // Use swagger-ui to view the components of the web api
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Environment registration Api");
                c.RoutePrefix = "swagger";
                c.DefaultModelExpandDepth(2);
                c.DefaultModelRendering(ModelRendering.Model);
                c.DefaultModelsExpandDepth(-1);
                c.DisplayOperationId();
                c.DisplayRequestDuration();
                c.DocExpansion(DocExpansion.None);
                c.EnableDeepLinking();
                c.MaxDisplayedTags(5);
                c.ShowExtensions();
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                //app.UseHsts();
            }

            app.UseCors();

            // Use VS SaaS middleware.
            app.UseVsSaaS(env.IsDevelopment());

            app.UseAuthentication();

            //app.UseHttpsRedirection();
            app.UseMvc();
        }
    }

    public class AddAuthorizationHeaderParameterOperationFilter : IOperationFilter
    {
        void IOperationFilter.Apply(Operation operation, OperationFilterContext context)
        {
            {
                var descriptor = context.ApiDescription.ActionDescriptor;
                var isAuthorized = descriptor.FilterDescriptors.Any(i => i.Filter is AuthorizeFilter);
                
                if (isAuthorized)
                {
                    if (operation.Parameters == null)
                    {
                        operation.Parameters = new List<IParameter>();
                    }
                    operation.Parameters.Add(new NonBodyParameter
                    {
                        Name = "Authorization",
                        In = "header",
                        Description = "bearer token",
                        Required = true,
                        Type = "string",
                        Default = "bearer "
                    });
                }
            }
        }
    }
}