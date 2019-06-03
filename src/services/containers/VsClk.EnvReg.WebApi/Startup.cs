using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsCloudKernel.Services.EnvReg.Models;
using Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore;
using Microsoft.VsCloudKernel.Services.EnvReg.Repositories;
using Microsoft.VsCloudKernel.Services.EnvReg.Repositories.DocumentDb;
using Microsoft.VsSaaS.Azure.Storage.FileShare;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Diagnostics.Health;
using StackExchange.Redis;
using VsClk.EnvReg.Repositories;
using Microsoft.VsCloudKernel.Services.EnvReg.WebApi.Provider;
using VsClk.EnvReg.Repositories.Support.HttpClient;
using VsClk.EnvReg.Repositories.HttpClient;
using Microsoft.VsCloudKernel.Services.EnvReg.WebApi.Authentication;

namespace Microsoft.VsCloudKernel.Services.EnvReg.WebApi
{
    public class Startup
    {
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
            });
            var mapper = config.CreateMapper();
            services.AddSingleton(mapper);

            ConfigureDbServices(services, appSettings);

            // Providers
            services.AddSingleton<ICurrentUserProvider, HttpContextCurrentUserProvider>();
            services.AddSingleton<IProfileCache, HttpContextProfileCache>();
            services.AddSingleton<IHttpClientProvider, HttpClientProvider>();

            // Repositories
            services.AddSingleton<IProfileRepository, HttpClientProfileRepository>();

            // Authentication
            services.AddVsClkCoreAuthenticationServices(HostEnvironment, appSettings);

            // VS SaaS services
            services.AddVsSaaSHosting(HostEnvironment, new LoggingBaseValues
            {
                CommitId = appSettings.GitCommit,
                ServiceName = "envreg",
            });

            services.AddFileShareProvider(options =>
            {
                options.AccountName = appSettings.StorageAccountName;
                options.AccountKey = appSettings.StorageAccountKey;
            });
            services.AddTransient<IStorageManager , StorageManager >();
        }

        private void ConfigureDbServices(IServiceCollection services, AppSettings appSettings)
        {
            // Document DB Repositories
            if (!HostEnvironment.IsDevelopment() || !appSettings.IsLocal)
            {
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
            else
            {
                // Use the mock db if we're developing locally
                services.AddSingleton<IEnvironmentRegistrationRepository, MockEnvironmentRegistrationRepository>();
            }
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                //app.UseHsts();
            }

            // Use VS SaaS middleware.
            app.UseVsSaaS(env.IsDevelopment());

            app.UseAuthentication();

            //app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}