using System.Linq;
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
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Diagnostics.Health;
using StackExchange.Redis;

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
            var config = new MapperConfiguration(cfg => {
                cfg.CreateMap<EnvironmentRegistration, EnvironmentRegistrationResult>();
                cfg.CreateMap<EnvironmentRegistrationInput, EnvironmentRegistration>();
            });
            var mapper = config.CreateMapper();
            services.AddSingleton(mapper);

            ConfigureDbServices(services, appSettings);

            // VS SaaS services
            services.AddVsSaaSHostingWithJwtBearerAuthentication(HostEnvironment,
                new LoggingBaseValues
                {
                    CommitId = appSettings.GitCommit,
                    ServiceName = "envreg",
                },
                authConfigOptions =>
                {
                    authConfigOptions.Authority = appSettings.AuthJwtAuthority;
                    if (!string.IsNullOrEmpty(appSettings.AuthJwtAudience))
                    {
                        authConfigOptions.AudienceAppId = appSettings.AuthJwtAudience;
                    }
                    else
                    {
                        authConfigOptions.AudienceAppIds = appSettings.AuthJwtAudiences?.Split(',');
                    }
                    authConfigOptions.IsEmailClaimRequired = false;
                });

            ConfigureAuthentication(services, appSettings);
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
            }
            else
            {
                // Use the mock db if we're developing locally
                services.AddSingleton<IEnvironmentRegistrationRepository, MockEnvironmentRegistrationRepository>();
            }
        }

        private void ConfigureAuthentication(IServiceCollection services, AppSettings appSettings)
        {
            // Add Data protection
            if (!HostEnvironment.IsDevelopment() || !appSettings.IsLocal)
            {
                var redis = ConnectionMultiplexer.Connect(appSettings.VsClkRedisConnectionString);
                services.AddDataProtection()
                    .SetApplicationName("VS Sass")
                    .PersistKeysToStackExchangeRedis(redis, "DataProtection-Keys");
            }
            else
            {
                services.AddDataProtection()
                    .SetApplicationName("VS Sass");
            }

            // Authentication
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = "proxy";
                options.DefaultChallengeScheme = "proxy";
            })

            .AddPolicyScheme("proxy", "Authorization Bearer or Cookie", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                    if (authHeader?.StartsWith("Bearer ") == true)
                    {
                        return JwtBearerDefaults.AuthenticationScheme;
                    }
                    return CookieAuthenticationDefaults.AuthenticationScheme;
                };
            })

            .AddCookie(options =>
            {
                options.LoginPath = "/login";
                options.Cookie.Name = ".AspNet.SharedCookie";
            })
            ;
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