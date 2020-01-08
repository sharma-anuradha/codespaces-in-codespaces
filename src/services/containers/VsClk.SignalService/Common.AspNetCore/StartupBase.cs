using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VsSaaS.AspNetCore.TelemetryProvider;
using Microsoft.VsSaaS.Common.Warmup;

namespace Microsoft.VsCloudKernel.SignalService
{
    public interface IStartupBase
    {
        string Environment { get; }
        bool IsDevelopmentEnv { get; }
        string ServiceId { get; }
        string Stamp { get; }
    }

    public abstract class StartupBase<AppSettings> : IStartupBase
        where AppSettings : AppSettingsBase
    {
        private const string ServiceValue = "signlr";

        private const string ServiceProperty = "Service";
        private const string ServiceIdProperty = "ServiceId";
        private const string ServiceTypeProperty = "ServiceType";
        private const string StampProperty = "Stamp";

        private Func<Type, ILogger> loggerFactory;

        private readonly IWebHostEnvironment _hostEnvironment;

        public StartupBase(ILoggerFactory loggerFactory, IWebHostEnvironment env)
        {
            this.loggerFactory = (t) => loggerFactory.CreateLogger(t.FullName);

            this._hostEnvironment = env;

            // Build configuration
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
#if DEBUG
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddJsonFile("appsettings.Debug.json", optional: true)
#else
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
#endif
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        public string Environment => this._hostEnvironment.EnvironmentName;
        public bool IsDevelopmentEnv => !this._hostEnvironment.IsProduction();
        public string ServiceId { get; private set; }
        public string Stamp { get; private set; }

        protected abstract string ServiceType { get; }
        protected abstract Type AppType { get; }

        protected ILogger Logger { get; private set; }
        protected IConfigurationSection AppSettingsConfiguration { get; private set; }
        protected ApplicationServicePrincipal ServicePrincipal { get; private set; }

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
            services.Configure<AppSettings>(AppSettingsConfiguration);

            // create a unique service Id
            ServiceId = Guid.NewGuid().ToString();

            // define the stamp
            Stamp = AppSettingsConfiguration.GetValue<string>(nameof(AppSettingsBase.Stamp));

            // If telemetry console provider is wanted
            if (AppSettingsConfiguration.GetValue<bool>(nameof(AppSettingsBase.UseTelemetryProvider)))
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
                        { StampProperty, Stamp }
                    };
                });

                var serviceProvider = services.BuildServiceProvider();
                this.loggerFactory = (t) => serviceProvider.GetService(t) as ILogger;
            }

            Logger = CreateLoggerInstance(AppType);
            Logger.LogInformation($"ConfigureServices -> env:{this._hostEnvironment.EnvironmentName}");

            // If privacy is enabled
            if (AppSettingsConfiguration.GetValue<bool>(nameof(AppSettingsBase.IsPrivacyEnabled)))
            {
                Logger.LogInformation("Privacy enabled...");
                // define our IServiceFormatProvider
                services.AddSingleton<IDataFormatProvider, DataFormatter>();
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
        }
    }
}