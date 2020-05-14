// <copyright file="StartupBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService.Common;
using Microsoft.VsSaaS.AspNetCore.TelemetryProvider;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Common.Warmup;

namespace Microsoft.VsCloudKernel.SignalService
{
    public abstract class StartupBase<TAppSettings> : IStartupBase
        where TAppSettings : AppSettingsBase
    {
        private const string RunningInAzureEnvironmentVariable = "RUNNING_IN_AZURE";

        private const string ServiceValue = "signlr";

        private const string UseTelemetryProviderOption = "useTelemetryProvider";
        private const string PrivacyEnabledOption = "privacyEnabled";
        private const string JsonRpcPortOption = "jsonRpcPort";

        private const string ServiceProperty = "Service";
        private const string ServiceIdProperty = "ServiceId";
        private const string ServiceTypeProperty = "ServiceType";
        private const string StampProperty = "Stamp";

        private const string CriticalExceptionMessage = "Critical exception found";

        private readonly IWebHostEnvironment hostEnvironment;

        private Func<Type, ILogger> loggerFactory;

        public StartupBase(
            IConfiguration configuration,
            IWebHostEnvironment hostEnvironment,
            ILoggerFactory loggerFactory)
        {
            this.loggerFactory = (t) => loggerFactory.CreateLogger(t.FullName);

            this.hostEnvironment = hostEnvironment;
            Configuration = configuration;
            CurrentAzureLocation = GetCurrentAzureLocation();
        }

        public IConfiguration Configuration { get; }

        public string Environment => this.hostEnvironment.EnvironmentName;

        public bool IsDevelopmentEnv => this.hostEnvironment.IsDevelopment();

        public string ServiceId { get; private set; }

        public string Stamp { get; private set; }

        public abstract string ServiceType { get; }

        public ServiceInfo ServiceInfo => new ServiceInfo(ServiceId, Stamp, ServiceType);

        public string PreferredLocation => CurrentAzureLocation.ToString();

        protected abstract Type AppType { get; }

        protected ILogger Logger { get; private set; }

        protected IConfigurationSection AppSettingsConfiguration { get; private set; }

        protected ApplicationServicePrincipal ServicePrincipal { get; private set; }

        /// <summary>
        /// Gets the current azure location.
        /// </summary>
        protected AzureLocation CurrentAzureLocation { get; }

        /// <summary>
        /// Checks whether the RUNNING_IN_AZURE variable is set.
        /// </summary>
        /// <returns>True if the variable is set to 'true'.</returns>
        protected static bool IsRunningInAzure()
        {
            return System.Environment.GetEnvironmentVariable(RunningInAzureEnvironmentVariable) == "true";
        }

        protected bool GetBoolConfiguration(string optionName)
        {
            return Configuration.GetValue<bool>(optionName);
        }

        protected ILogger CreateLoggerInstance(Type type)
        {
            Requires.NotNull(this.loggerFactory, "logger facatory not initialized");
            return this.loggerFactory(typeof(ILogger<>).MakeGenericType(type));
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        protected void ConfigureCommonServices(IServiceCollection services)
        {
            // Configuration
            AppSettingsConfiguration = Configuration.GetSection("AppSettings");
            services.Configure<AppSettingsBase>(AppSettingsConfiguration);
            services.Configure<TAppSettings>(AppSettingsConfiguration);

            // create a unique service Id
            ServiceId = Guid.NewGuid().ToString();

            // define the stamp
            Stamp = AppSettingsConfiguration.GetValue<string>(nameof(AppSettingsBase.Stamp));

            // If telemetry console provider is wanted
            if (GetBoolConfiguration(UseTelemetryProviderOption) || AppSettingsConfiguration.GetValue<bool>(nameof(AppSettingsBase.UseTelemetryProvider)))
            {
                // inject the Telemetry logger provider
                services.ReplaceConsoleTelemetry((opts) =>
                {
                    // Our options on every telemetry log
                    opts.FactoryOptions = new Dictionary<string, object>()
                    {
                        { ServiceProperty, ServiceValue },
                        { ServiceIdProperty, ServiceId },
                        { ServiceTypeProperty, ServiceType },
                        { StampProperty, Stamp },
                    };

                    bool failFast = false;
                    opts.ExceptionProvider = ex =>
                    {
                        if (!failFast && IsAggregateCriticalException(ex))
                        {
                            failFast = true;
                            Task.Run(async () =>
                            {
                                Logger.LogError(ex, $"Fail fast due to:{CriticalExceptionMessage}");

                                // wait to have our telemetry to upload the original exception and this last event logging
                                await Task.Delay(500);
                                System.Environment.FailFast(CriticalExceptionMessage, ex);
                            });
                        }

                        return null;
                    };
                });

                var serviceProvider = services.BuildServiceProvider();
                this.loggerFactory = (t) => serviceProvider.GetService(t) as ILogger;
            }

            Logger = CreateLoggerInstance(AppType);
            Logger.LogInformation($"ConfigureServices -> env:{this.hostEnvironment.EnvironmentName}");

            // If privacy is enabled
            if (GetBoolConfiguration(PrivacyEnabledOption) || AppSettingsConfiguration.GetValue<bool>(nameof(AppSettingsBase.IsPrivacyEnabled)))
            {
                Logger.LogInformation("Privacy enabled...");

                // define our IServiceFormatProvider
                services.AddSingleton<IDataFormatProvider, DataFormatter>();
            }

            // override json-rpc port if needed
            int jsonRpcPort = Configuration.GetValue<int>(JsonRpcPortOption, -1);
            if (jsonRpcPort != -1)
            {
                AppSettingsConfiguration[nameof(AppSettingsBase.JsonRpcPort)] = jsonRpcPort.ToString();
            }

            // DI for ApplicationServicePrincipal
            ServicePrincipal = Configuration.GetSection(nameof(ApplicationServicePrincipal)).Get<ApplicationServicePrincipal>();
            services.AddSingleton((srvcProvider) => ServicePrincipal);

            // define list of IAsyncWarmup implementation available
            var warmupServices = new List<IAsyncWarmup>();
            services.AddSingleton<IList<IAsyncWarmup>>((srvcProvider) => warmupServices);

            // define list of IHealthStatusProvider implementation available
            var healthStatusProviders = new List<IHealthStatusProvider>();
            services.AddSingleton<IList<IHealthStatusProvider>>((srvcProvider) => healthStatusProviders);

            // define our overall warmup service
            services.AddSingleton<WarmupService>();

            // define our overall health service
            services.AddSingleton<HealthService>();

            // DI this instance
            services.AddSingleton<IStartupBase>(this);
            services.AddSingleton(AppType, this);

            // allows tracking service perf counters.
            services.AddSingleton<IServiceCounters, ServiceCounters>();
        }

        private static bool IsAggregateCriticalException(Exception exception)
        {
            return IsCriticalException(exception) || exception.GetInnerExceptions().Any(e => IsCriticalException(e));
        }

        private static bool IsCriticalException(Exception exception)
        {
            return exception is StackOverflowException
                || exception is OutOfMemoryException
                || exception is InsufficientExecutionStackException
                || exception is InsufficientMemoryException;
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
                    Logger.LogError(e, "Failed to get current location");

                    // If running in Azure, we must know our location in order to properly load configuraiton data.
                    throw;
                }
            }
            else
            {
                var locationEnvVar = System.Environment.GetEnvironmentVariable("AZURE_LOCATION");
                if (!string.IsNullOrEmpty(locationEnvVar))
                {
                    var azureLocation = Enum.Parse<AzureLocation>(locationEnvVar, ignoreCase: true);
                    return azureLocation;
                }

                // Default location for localhost development.
                return AzureLocation.WestUs2;
            }
        }
    }
}