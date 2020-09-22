// <copyright file="CommonStartupBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.VsSaaS.Azure.Maps;
using Microsoft.VsSaaS.Caching;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.Repository.AzureCosmosDb;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Developer.DevStampLogger;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Metrics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Warmup;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore
{
    /// <summary>
    /// Shared base for Startup in both front-end and back-end.
    /// </summary>
    /// <typeparam name="TAppSettings">The appsettings type.</typeparam>
    public class CommonStartupBase<TAppSettings>
        where TAppSettings : AppSettingsBase, new()
    {
        private const string RunningInAzureEnvironmentVariable = "RUNNING_IN_AZURE";
        private const string OverrideAppSettingsJsonEnvironmentVariable = "OVERRIDE_APPSETTINGS_JSON";
        private const string AppSettingsSectionName = "AppSettings";
        private const string AppSecretsSectionName = "AppSecrets";

        /// <summary>
        /// Initializes a new instance of the <see cref="CommonStartupBase{TAppSettings}"/> class.
        /// </summary>
        /// <param name="hostingEnvironment">The aspnetcore hosting environment.</param>
        /// <param name="serviceName">The service name.</param>
        public CommonStartupBase(
            IWebHostEnvironment hostingEnvironment,
            string serviceName)
        {
            Requires.NotNull(hostingEnvironment, nameof(hostingEnvironment));
            Requires.NotNullOrEmpty(serviceName, nameof(serviceName));

            HostingEnvironment = hostingEnvironment;
            ServiceName = serviceName;

            // Initialize the current azure location -- it will default to westus2 if not running in azure.
            CurrentAzureLocation = GetCurrentAzureLocation();

            // All appsettings.*.json files are loaded from this location.
            var settingsRelativePath = GetSettingsRelativePath();

            // This function loads a number of appsettings*.json files. Some are common for
            // all configurations. Appsettings that are specific to environment/instance
            // are designated by using an infix such as "dev", "ppe-rel", or "prod-rel" in the filename.
            //
            // The infix is used with these primary appsettings settings files:
            //      appsettings.{infix}.json
            //      appsettings.subscriptions.{infix}.json
            //
            // We determine the infix based on the current hosting environment. For production, we also
            // need to differentiate release vs canary. The canary release pipeline sets the
            // appsettings override file to "appsettings.prod-can.json". If that
            // override file is specified, instead of treating it as a real override, we instead
            // change the infix from "prod-rel" to "prod-can". This will yield both
            // "appsettings.prod-can.json" and "appsettings.subscriptions.prod-can.jsonc".
            // In this case, the override file is now redundant is redundant and it is nulled out.

            // Get the override file, if any, and test if it is canary.
            var overrideAppSettingsJsonFile = GetOverrideAppSettingsJsonFile();
            var isCanary = overrideAppSettingsJsonFile == "appsettings.prod-can.json";

            // Get the infix designation and compute the standard appsettings filenames.
            var infix = GetAppSettingsInfixName(isCanary);
            var appsettingsInfixFile = $"appsettings.{infix}.json";
            var appsettingsSubscriptionsInfixFile = $"appsettings.subscriptions.{infix}.jsonc";

            // If the override file is equal to the standard file (as with canary), we don't
            // want to load the override file a second time; so null it out.
            if (appsettingsInfixFile.Equals(overrideAppSettingsJsonFile, StringComparison.OrdinalIgnoreCase))
            {
                overrideAppSettingsJsonFile = null;
            }

            var builder = new ConfigurationBuilder()
                .SetBasePath(hostingEnvironment.ContentRootPath)
                .AddJsonFile(AddAppSettingsJsonFile($"{settingsRelativePath}appsettings.json"), optional: false, reloadOnChange: true)
                .AddJsonFile(AddAppSettingsJsonFile($"{settingsRelativePath}appsettings.secrets.json"), optional: true)
                .AddJsonFile(AddAppSettingsJsonFile($"{settingsRelativePath}appsettings.images.json"), optional: false)
                .AddJsonFile(AddAppSettingsJsonFile($"{settingsRelativePath}appsettings.commitId.json"), optional: true);

            // Add the {env}-{instance} specific appsettings and appsettings.subscriptions files.
            builder.AddJsonFile(AddAppSettingsJsonFile($"{settingsRelativePath}{appsettingsInfixFile}"), optional: false);
            builder.AddJsonFile(AddAppSettingsJsonFile($"{settingsRelativePath}{appsettingsSubscriptionsInfixFile}"), optional: false);

            // Get the optional override appsettings file.
            if (overrideAppSettingsJsonFile != null)
            {
                builder.AddJsonFile(AddAppSettingsJsonFile($"{settingsRelativePath}{overrideAppSettingsJsonFile}"), optional: false);
            }
            else if (hostingEnvironment.IsDevelopment())
            {
                // Get the default override appsettings file as dev-ci to not break cenarios for development environment.
                builder.AddJsonFile(AddAppSettingsJsonFile($"{settingsRelativePath}appsettings.dev-ci.json"), optional: false);
            }

            // Load the local file if not running in azure.
            if (!IsRunningInAzure() && hostingEnvironment.IsDevelopment())
            {
                builder.AddJsonFile(AddAppSettingsJsonFile($"{settingsRelativePath}appsettings.local.json"), optional: true);

                // Loading from user profile so that it will even work with git clean or a new repo.
                var userAppSettings = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "CEDev", "appsettings.json");
                builder.AddJsonFile(AddAppSettingsJsonFile(userAppSettings), optional: true);
            }

            builder.AddEnvironmentVariables();
            builder.AddEnvironmentVariables(prefix: "VSCS_");

            AddServiceSpecificAppConfigFiles(builder, settingsRelativePath, infix);

            Configuration = builder.Build();
        }

        /// <summary>
        /// Gets the service name.
        /// </summary>
        public string ServiceName { get; }

        /// <summary>
        /// Gets the log-message base for LogInfo.
        /// </summary>
        protected string LogMessageBase => $"startup_{ServiceName.ToLowerInvariant()}";

        /// <summary>
        /// Gets the configuration instance.
        /// </summary>
        protected IConfiguration Configuration { get; }

        /// <summary>
        /// Gets the hosting environment.
        /// </summary>
        protected IWebHostEnvironment HostingEnvironment { get; }

        /// <summary>
        /// Gets or sets the appsettings instance.
        /// </summary>
        protected TAppSettings AppSettings { get; set; }

        /// <summary>
        /// Gets the current azure location.
        /// </summary>
        protected AzureLocation CurrentAzureLocation { get; }

        /// <summary>
        /// Gets the control-plane azure resource acessor.
        /// </summary>
        protected IControlPlaneAzureResourceAccessor ControlPlaneAzureResourceAccessor { get; private set; }

        /// <summary>
        /// Gets the list of app settings json files for logging.
        /// </summary>
        protected List<string> AppSettingsJsonFiles { get; } = new List<string>();

        /// <summary>
        /// Checks whether the RUNNING_IN_AZURE variable is set.
        /// </summary>
        /// <returns>True if the variable is set to 'true'.</returns>
        protected static bool IsRunningInAzure()
        {
            return Environment.GetEnvironmentVariable(RunningInAzureEnvironmentVariable) == "true";
        }

        /// <summary>
        /// Load and configure the AppSettings.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The <paramref name="services"/> instance.</returns>
        protected TAppSettings ConfigureAppSettings(IServiceCollection services)
        {
            var appSettingsConfiguration = Configuration.GetSection(AppSettingsSectionName);
            AppSettings = appSettingsConfiguration.Get<TAppSettings>();
            services.Configure<TAppSettings>(appSettingsConfiguration);
            services.AddSingleton(AppSettings);
            return AppSettings;
        }

        /// <summary>
        /// Configures the secret provider.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configSection">The app secrets configuration section.</param>
        protected virtual void ConfigureAppSecrets(
            IServiceCollection services,
            IConfigurationSection configSection)
        {
            var appSecrets = configSection.Get<CommonAppSecretsProvider>();
            if (appSecrets != null)
            {
                // Some integration tests can run without any secrets.
                services.TryAddSingleton<ISecretProvider>(appSecrets);
            }
        }

        /// <summary>
        /// Configures the hostname for local development.
        /// </summary>
        /// <param name="hostname">The hostname.</param>
        protected void ConfigureLocalHostname(string hostname)
        {
            if (string.IsNullOrEmpty(AppSettings.ControlPlaneSettings.DnsHostName))
            {
                AppSettings.ControlPlaneSettings.DnsHostName = hostname;
            }

            foreach (var stamp in AppSettings.ControlPlaneSettings.Stamps)
            {
                if (string.IsNullOrEmpty(stamp.Value.DnsHostName))
                {
                    stamp.Value.DnsHostName = hostname;
                }
            }
        }

        /// <summary>
        /// Add various DI services common to all VSO services.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="appSettings">App setting base.</param>
        /// <param name="kustoStreamLogging">Kusto stream logging for dev stamps.</param>
        /// <param name="loggingBaseValues">The common logging base values.</param>
        protected void ConfigureCommonServices(IServiceCollection services, AppSettingsBase appSettings, bool? kustoStreamLogging, out LoggingBaseValues loggingBaseValues)
        {
            services.AddSingleton(appSettings);

            if (!kustoStreamLogging.HasValue)
            {
                kustoStreamLogging = AppSettings.DeveloperPersonalStamp && AppSettings.DeveloperKusto;
            }

            services.AddSingleton<IDiagnosticsLoggerFactory>(x => ActivatorUtilities.CreateInstance<DeveloperStampDiagnosticsLoggerFactory>(x, kustoStreamLogging, AppSettings.RedirectStandardOutToLogsDirectory));

            var productInfo = new ProductInfoHeaderValue(
                ServiceName, Assembly.GetExecutingAssembly().GetName().Version.ToString());
            services.AddSingleton(productInfo);

            ConfigureAppSecrets(services, Configuration.GetSection(AppSecretsSectionName));

            services.AddApplicationServicePrincipal(AppSettings.ApplicationServicePrincipal);

            services.AddControlPlaneInfo(AppSettings.ControlPlaneSettings);

            services.AddControlPlaneAzureResourceAccessor();

            // Note: there is a AzureClientFactoryMock that is no longer configured.
            // It would be added back here if we decide to get mocks working again.
            services.AddAzureClientFactory();
            services.AddControlPlaneAzureClientFactory();

            services.AddCurrentLocationProvider(CurrentAzureLocation);

            services.AddSystemCatalog(
                AppSettings.DataPlaneSettings,
                AppSettings.SkuCatalogSettings,
                AppSettings.PlanSkuCatalogSettings,
                AppSettings.QuotaFamilySettings,
                AppSettings.ApplicationServicePrincipal);

            loggingBaseValues = new LoggingBaseValues
            {
                ServiceName = ServiceName,
                CommitId = AppSettings.GitCommit,
                AdditionalValues = new Dictionary<string, string>
                {
                    { "BuildId", AppSettings.BuildId },
                    { "BuildNumber", AppSettings.BuildNumber },
                },
            };

            services.AddTransient(serviceProvider =>
            {
                var loggerFactory = serviceProvider.GetService<IDiagnosticsLoggerFactory>();
                var logValueSet = serviceProvider.GetService<LogValueSet>();
                return loggerFactory.New(logValueSet);
            });

            services.AddSingleton<ITriggerWarmup, TriggerWarmup>();
            services.AddSingleton<ITaskHelper, TaskHelper>();
            services.AddSingleton<IImageUrlGenerator, BlobImageUrlGenerator>();

            // Setup configuration
            services.AddVsoDocumentDbCollection<SystemConfigurationRecord, ISystemConfigurationRepository, CachedCosmosDbSystemConfigurationRepository>(
                CachedCosmosDbSystemConfigurationRepository.ConfigureOptions);
            services.AddVsoDocumentDbCollection<SystemConfigurationRecord, ICachedSystemConfigurationRepository, CachedCosmosDbSystemConfigurationRepositoryV2>(
                CachedCosmosDbSystemConfigurationRepositoryV2.ConfigureOptions);
            services.AddSingleton<ISystemConfiguration, PersistedSystemConfigurationV2>();
            services.AddSingleton<ICachedSystemConfiguration, PersistedSystemConfigurationV2>();
            services.AddSingleton<IConfigurationKeyGenerator, ConfigurationKeyGenerator>();
            services.AddSingleton<IConfigurationReader, ConfigurationReader>();
            services.AddSingleton<CertificateKeyVaultSecretCache>();
            services.AddSingleton<ICertificateKeyVaultSecretCache>(x => x.GetRequiredService<CertificateKeyVaultSecretCache>());
            services.AddSingleton<IRefreshKeyVaultSecretCache>(x => x.GetRequiredService<CertificateKeyVaultSecretCache>());

            // Use TryAddSingleton to allow callers to override this implementation by calling AddSingleton before calling this method
            services.TryAddSingleton<ICurrentImageInfoProvider, CurrentImageInfoProvider>();

            // Metrics Logger
            services.TryAddSingleton<IManagedCache, InMemoryManagedCache>();
            services.AddAzureMaps(options =>
            {
                var controlPlaneInfo = ApplicationServicesProvider.GetRequiredService<IControlPlaneInfo>();
                var controlPlaneResourceAccessor = ApplicationServicesProvider.GetRequiredService<IControlPlaneAzureResourceAccessor>();
                options.AccountName = controlPlaneInfo.InstanceMapsAccountName;
                options.ResourceGroupName = controlPlaneInfo.InstanceResourceGroupName;
                options.SubscriptionId = controlPlaneResourceAccessor.GetCurrentSubscriptionIdAsync().Result;
            });
            services.AddMetrics(options =>
            {
                options.MdsdEventSource = AppSettings.MetricsLoggerMdsdEventSource;
            });

            // Job Scheduler support
            services.AddJobScheduler();

            // Job scheduler lease support
            services.AddSingleton<IJobSchedulerLease, JobSchedulerLease>();
            services.AddSingleton<IJobSchedulerLeaseProvider, JobSchedulerLeaseProvider>();

            // Job Scheduler Feature Flags
            services.AddSingleton<IJobSchedulerFeatureFlags, JobSchedulerFeatureFlags>();

            // Job Queues support
            services.AddSingleton<IQueueFactory, StorageQueueFactory>();
            services.AddSingleton<IJobQueueProducerFactory, JobQueueProducerFactory>();
            services.AddSingleton<IJobQueueConsumerFactory, JobQueueConsumerFactory>();
            services.AddSingleton<IJobQueueProducerFactoryHelpers, JobQueueProducerFactoryHelpers>();
        }

        /// <summary>
        /// All appsettings.*.json files are loaded from this location.
        /// </summary>
        /// <returns>The settings path or empty string.</returns>
        protected virtual string GetSettingsRelativePath()
        {
            var dirChar = Path.DirectorySeparatorChar.ToString();
            var settingsRelativePath = IsRunningInAzure() ? string.Empty : Path.GetFullPath(
                Path.Combine(HostingEnvironment.ContentRootPath, "..", "..", "..", "..", $"Settings{dirChar}"));
            return settingsRelativePath;
        }

        /// <summary>
        /// Add settings file to a list so we can output the configuration for debugging purposes.
        /// </summary>
        /// <param name="path">The settings file path.</param>
        /// <returns>The same settings file path.</returns>
        protected string AddAppSettingsJsonFile(string path)
        {
            AppSettingsJsonFiles.Add(path);
            return path;
        }

        /// <summary>
        /// Extension point to add more service specific settings files. Used for example by port forwarding agent.
        /// </summary>
        /// <param name="builder">The aspnet core configuration builder.</param>
        /// <param name="settingsRelativePath">The relative path to settings files for current environment.</param>
        /// <param name="infix">The environment infix.</param>
        protected virtual void AddServiceSpecificAppConfigFiles(IConfigurationBuilder builder, string settingsRelativePath, string infix)
        {
        }

        /// <summary>
        /// Common application configuration.
        /// </summary>
        /// <param name="app">The application builder.</param>
        protected void ConfigureAppCommon(IApplicationBuilder app)
        {
            // Initialize global services as early as possible...
            ApplicationServicesProvider.TrySetServiceProvider(app.ApplicationServices);

            // Save this for configuration callbacks that require cosmos db accounts.
            ControlPlaneAzureResourceAccessor = app.ApplicationServices.GetRequiredService<IControlPlaneAzureResourceAccessor>();

            // Add the service/stamp info to the base value set
            var controlPlaneInfo = app.ApplicationServices.GetRequiredService<IControlPlaneInfo>();
            var baseLogValueSet = app.ApplicationServices.GetRequiredService<LogValueSet>();
            baseLogValueSet.Add("ServiceEnvironment", controlPlaneInfo.EnvironmentResourceGroupName);
            baseLogValueSet.Add("ServiceInstance", controlPlaneInfo.InstanceResourceGroupName);
            baseLogValueSet.Add("ServiceStamp", controlPlaneInfo.Stamp.StampResourceGroupName);
            baseLogValueSet.Add("ServiceLocation", controlPlaneInfo.Stamp.Location.ToString().ToLowerInvariant());
            baseLogValueSet.Add("KubernetesPodName", System.Net.Dns.GetHostName());

            // Emit the startup settings to logs for diagnostics.
            var logger = app.ApplicationServices.GetRequiredService<IDiagnosticsLogger>();
            LogSettings(logger);

            // Load and validate the system catelog
            try
            {
                app.UseSystemCatalog();
            }
            catch (Exception ex)
            {
                logger.LogException($"{LogMessageBase}_failed", ex);
                throw;
            }
        }

        /// <summary>
        /// Starts the warm up of the service.
        /// </summary>
        /// <param name="app">Application builder.</param>
        protected void Warmup(IApplicationBuilder app)
        {
            var warmup = app.ApplicationServices.GetRequiredService<ITriggerWarmup>();
            warmup.Start();
        }

        private static string GetOverrideAppSettingsJsonFile()
        {
            var overrideAppSettingsJsonFile = Environment.GetEnvironmentVariable(OverrideAppSettingsJsonEnvironmentVariable);

            if (string.IsNullOrEmpty(overrideAppSettingsJsonFile) ||
                overrideAppSettingsJsonFile.Equals("\"false\"", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // Helm adds quotes in strange ways. Linux doesn't like them.
            overrideAppSettingsJsonFile = overrideAppSettingsJsonFile.Trim('"');
            return overrideAppSettingsJsonFile;
        }

        /// <summary>
        /// Emit appsettings, etc., to the diagnostics logger.
        /// This has the side-effect of serializaing AppSettings, which could fail if required properties are not set.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        private void LogSettings(IDiagnosticsLogger logger)
        {
            try
            {
                logger
                    .FluentAddValue("appSettingJsonFiles", string.Join(",", AppSettingsJsonFiles))
                    .FluentAddValue("appSettings", JsonConvert.SerializeObject(
                        AppSettings,
                        new JsonSerializerSettings { MaxDepth = 10, }).Replace("\"", "'"))
                    .FluentAddValue("currentAzureLocation", CurrentAzureLocation)
                    .FluentAddValue("environmentName", HostingEnvironment.EnvironmentName)
                    .LogInfo(LogMessageBase);
            }
            catch (Exception ex)
            {
                logger.LogException($"{LogMessageBase}_bad_settings", ex);
                throw;
            }
        }

        private string GetAppSettingsInfixName(bool isCanary)
        {
            /* Future: If needed, let deployment set an environment variable to override this default.
             */

            if (HostingEnvironment.IsDevelopment())
            {
                return "dev";
            }
            else if (HostingEnvironment.IsStaging())
            {
                return "ppe-rel";
            }
            else if (HostingEnvironment.IsProduction())
            {
                return isCanary ? "prod-can" : "prod-rel";
            }

            throw new NotSupportedException($"The hosting environment '{HostingEnvironment.EnvironmentName}' is not supported.");
        }

        private AzureLocation GetCurrentAzureLocation()
        {
            if (IsRunningInAzure())
            {
                try
                {
                    var currentAzureLocation = Retry
                        .DoAsync(async (attemptNumber) => await AzureInstanceMetadata.GetCurrentLocationAsync())
                        .GetAwaiter()
                        .GetResult();

                    return Enum.Parse<AzureLocation>(currentAzureLocation, ignoreCase: true);
                }
                catch (Exception e)
                {
                    var logger = new DefaultLoggerFactory().New(new LogValueSet()
                    {
                        { LoggingConstants.Service, ServiceName },
                    });
                    logger.AddExceptionInfo(e).LogError("error_querying_azure_region_from_instance_metadata");

                    // If running in Azure, we must know our location in order to properly load configuraiton data.
                    throw;
                }
            }
            else
            {
                var locationEnvVar = Environment.GetEnvironmentVariable("AZURE_LOCATION");
                if (!string.IsNullOrEmpty(locationEnvVar))
                {
                    var azureLocation = Enum.Parse<AzureLocation>(locationEnvVar, ignoreCase: true);
                    return azureLocation;
                }

                // Default location for localhost development.
                return AzureLocation.WestUs2;
            }
        }

        private class JobSchedulerLeaseProvider : JobSchedulerLeaseProviderBase
        {
            public JobSchedulerLeaseProvider(
                IClaimedDistributedLease claimedDistributedLease,
                IResourceNameBuilder resourceNameBuilder,
                AppSettingsBase appSettingsBase)
                : base(claimedDistributedLease, resourceNameBuilder)
            {
                AppSettings = Requires.NotNull(appSettingsBase, nameof(appSettingsBase));
            }

            protected override string LeaseContainerName => AppSettings.ClaimDistributedContainerName;

            private AppSettingsBase AppSettings { get; }
        }
    }
}
