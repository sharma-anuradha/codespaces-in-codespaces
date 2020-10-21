// <copyright file="ConfigurationWriterCommand.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.IO;
using System.Linq;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil
{
    /// <summary>
    /// Configuration writer commands.
    /// </summary>
    [Verb("write-config", HelpText = "Configuration Writer Commands.")]
    public class ConfigurationWriterCommand : SystemConfigurationCommandBase
    {
        /// <summary>
        /// Gets or sets a value indicating wheather we are using interactive mode or not
        /// </summary>
        [Option('i', "interactive", Default = false, HelpText = "Select if the interactive mode should be used", Required = false)]
        public bool InteractiveMode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating configuration context
        /// </summary>
        [Option('c', "context", Default = "default", HelpText = "Configuration context to use: \"default, subscription, plane or user\"", Required = false)]
        public string Context { get; set; }

        /// <summary>
        /// Gets or sets a value indicating configuration type
        /// </summary>
        [Option('t', "type", HelpText = "Configuration type:  \"feature, quota or setting\"", Required = false)]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets a value indicating component name
        /// </summary>
        [Option('n', "comp-name", HelpText = "Component name/feature name e.g. environmentmanager, planmanager, queue-resources", Required = false)]
        public string ComponentName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating configuration name
        /// </summary>
        [Option('s', "conf-name", HelpText = "Configuration name/setting name e.g. max-plans-per-sub, max-environments-per-plan or \"enabled\" for a feature ", Required = false)]
        public string ConfigurationName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating subscription id to use
        /// </summary>
        [Option("sub", HelpText = "Subscription id", Required = false)]
        public string Subscription { get; set; }

        /// <summary>
        /// Gets or sets a value indicating plan name
        /// </summary>
        [Option("plan", HelpText = "Plan name", Required = false)]
        public string Plan { get; set; }

        /// <summary>
        /// Gets or sets a value indicating user id
        /// </summary>
        [Option("user", HelpText = "User id", Required = false)]
        public string User { get; set; }

        /// <summary>
        /// Gets or sets a value indicating value of the key that needs to be added to the database
        /// </summary>
        [Option("value", HelpText = "Value of the key to be added in the database", Required = false)]
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets a value indicating comment for the key
        /// </summary>
        [Option("comment", HelpText = "Description of the key to be added in the database", Required = false)]
        public string Comment { get; set; }

        /// <summary>
        /// Gets or sets a value indicating wheather we are using interactive mode or not
        /// </summary>
        [Option('g', "global", Default = false, HelpText = "Select if you want a global scoped key i.e. service level key which is applicable to all the regions", Required = false)]
        public bool Global { get; set; }

        /// <summary>
        /// Gets or sets a value indicating wheather we are using interactive mode or not
        /// </summary>
        [Option('r', "regional", Default = false, HelpText = "Select if you want a region scoped key i.e. key which is applicable to a particular regions", Required = false)]
        public bool Regional { get; set; }

        /// <inheritdoc/>
        protected override void ExecuteCommand(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            if (InteractiveMode)
            {
                InteractiveModeExecution(services, stdout, stderr);
            }
            else
            {
                NonInteractiveModeExecution(services, stdout, stderr);
            }
        }

        private void NonInteractiveModeExecution(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            ValidateNonInteractiveInput();

            var configurationContext = BuildConfigurationContext();
            var configurationType = Type.ToLower() switch
            {
                "feature" => ConfigurationType.Feature,
                "quota" => ConfigurationType.Quota,
                "setting" => ConfigurationType.Setting,
                _ => throw new InvalidOperationException("Only allowed values are : feature, quota or setting")
            };

            var key = GenerateKey(services, configurationContext, configurationType, ComponentName, ConfigurationName);
            
            if (Verbose)
            {
                stdout.WriteLine($"Context : {Context}");
                stdout.WriteLine($"Configuration Type : {configurationType}");
                stdout.WriteLine($"Component name : {ComponentName}");
                stdout.WriteLine($"Configuration Name : {ConfigurationName}");
                stdout.WriteLine($"Global used : {Global}");
                stdout.WriteLine($"Regional Used : {Regional}");
                stdout.WriteLine($"Backend DB : {UseBackEnd}");
            }

            stdout.WriteLine($"Key : \"{key}\"");
            stdout.WriteLine($"Value : \"{Value}\"");
            stdout.WriteLine($"Comment : \"{Comment}\"");

            UpdateSystemConfigurationAsync(services, key, Value, stdout, stderr, Comment).Wait();
        }

        private void ValidateNonInteractiveInput()
        {
            if (string.IsNullOrEmpty(Type))
            {
                throw new InvalidOperationException("Configuration type is missing. Provide it using -t or --type");
            }

            if (string.IsNullOrEmpty(ComponentName))
            {
                throw new InvalidOperationException("Component name/feature name is missing. Provide it using -n or --comp-name");
            }

            if (string.IsNullOrEmpty(ConfigurationName))
            {
                throw new InvalidOperationException("Configuration name/setting/ name is missing. Enter \"enabled\" for feature. Provide it using -s or --conf-name");
            }

            if (string.IsNullOrEmpty(Value))
            {
                throw new InvalidOperationException("Value to be used is missing. Provide it using --value");
            }

            if (string.IsNullOrEmpty(Comment))
            {
                throw new InvalidOperationException("Comment to be used is missing. Provide it using --comment");
            }
        }

        private string GenerateKey(IServiceProvider services, ConfigurationContext context, ConfigurationType configurationType, string componentName, string configurationName)
        {
            var configurationScopeGenerator = services.GetRequiredService<IConfigurationScopeGenerator>();

            var scopes = configurationScopeGenerator.GetScopes(context);

            if (Global == Regional)
            {
                throw new InvalidOperationException("Exactly one of global and region flag should be selected. Use either -g (for global) or -r (for regional)");
            }

            // cannot use First or Last because some scope like plan generate upto 4 keys.
            var regionalKey = ConfigurationHelpers.GetCompleteKey(scopes.ElementAt(0), configurationType, componentName, configurationName);
            var globalKey = ConfigurationHelpers.GetCompleteKey(scopes.ElementAt(1), configurationType, componentName, configurationName);
            return (Regional) ? regionalKey : globalKey;
        }

        private ConfigurationContext BuildConfigurationContext()
        {
            var context = ConfigurationContextBuilder.GetDefaultContext();

            switch (Context.ToLower())
            {
                case "default":
                    return context;
                case "subscription":
                    {
                        if (string.IsNullOrEmpty(Subscription))
                        {
                            throw new InvalidOperationException("Subscription is required for selected context. Provide it using --sub");
                        }

                        context.ServiceScopeApplicable = false;
                        context.RegionScopeApplicable = false;
                        context.SubscriptionId = Subscription;
                    }
                    
                    break;
                case "plan":
                    {
                        if (string.IsNullOrEmpty(Subscription) || string.IsNullOrEmpty(Plan))
                        {
                            throw new InvalidOperationException("Subscription and plan are required for selected context. Provide them using --sub and --plan");
                        }

                        context.ServiceScopeApplicable = false;
                        context.RegionScopeApplicable = false;
                        context.SubscriptionId = Subscription;
                        context.PlanId = Plan;
                    }

                    break;
                case "user":
                    {
                        if (string.IsNullOrEmpty(User))
                        {
                            throw new InvalidOperationException("User id is required for selected context. Provide it using --user");
                        }

                        context.ServiceScopeApplicable = false;
                        context.RegionScopeApplicable = false;
                        context.UserId = User;
                    }

                    break;
                default:
                    throw new InvalidOperationException("Only allowed values are  \"default, subscription, plane or user\"");
            }

            return context;
        }

        private void InteractiveModeExecution(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            stdout.WriteLine("\nSelect one of the following : \n1. Generate key \n2. Add/Update/Delete key");
            var optionChoice = Console.ReadLine();
            int option = Convert.ToInt32(optionChoice);

            switch (option)
            {
                case 1:
                    ExecuteKeyGeneration(services, stdout, stderr);
                    break;
                case 2:
                    ExecuteKeyUpdation(services, stdout, stderr);
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        private void ExecuteKeyUpdation(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            stdout.WriteLine("\nEnter your key:");
            var key = Console.ReadLine();

            stdout.WriteLine("\nEnter your value (leave blank by pressing enter to delete the key):");
            var value = Console.ReadLine();
            var val = string.IsNullOrEmpty(value) ? null : value;

            string comment = default;
            if (val != null)
            {
                stdout.WriteLine("\nEnter a comment describing the key:");
                comment = Console.ReadLine();

                if (string.IsNullOrEmpty(comment))
                {
                    throw new InvalidOperationException();
                }
            }

            UpdateSystemConfigurationAsync(services, key, val, stdout, stderr, comment).Wait();
        }

        private void ExecuteKeyGeneration(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            var configurationType = GetConfigurationType(stdout);
            var (componentName, configurationName) = GetComponentAndConfigurationName(stdout, configurationType);
            var context = GetConfigurationContext(stdout, stderr);
            PrintAllTheKeys(services, context, configurationType, componentName, configurationName, stdout);
        }

        private ConfigurationType GetConfigurationType(TextWriter stdout)
        {
            stdout.WriteLine("\nEnter your configuration type : \n1. Feature \n2. Quota \n3. Setting");
            var type = Console.ReadLine();
            int choice = Convert.ToInt32(type);

            ConfigurationType configurationType = choice switch
            {
                1 => ConfigurationType.Feature,
                2 => ConfigurationType.Quota,
                3 => ConfigurationType.Setting,
                _ => throw new InvalidOperationException()
            };

            return configurationType;
        }

        private void PrintAllTheKeys(IServiceProvider services, ConfigurationContext context, ConfigurationType configurationType, string componentName, string configurationName, TextWriter stdout)
        {
            var configurationScopeGenerator = services.GetRequiredService<IConfigurationScopeGenerator>();

            var scopes = configurationScopeGenerator.GetScopes(context);

            stdout.WriteLine("\nKeys in the decreasing order of priority - one at the top will have highest preference");
            var i = 1;
            foreach (var scope in scopes)
            {
                var key = ConfigurationHelpers.GetCompleteKey(scope, configurationType, componentName, configurationName);
                stdout.WriteLine($"{i++}. {key}");
            }
        }

        private (string componentName, string configurationName) GetComponentAndConfigurationName(TextWriter stdout, ConfigurationType configurationType)
        {
            var componentName = string.Empty;
            var configurationName = string.Empty;

            switch (configurationType)
            {
                case ConfigurationType.Feature:
                    {
                        stdout.WriteLine("\nEnter Name of the feature.");
                        componentName = Console.ReadLine();
                        configurationName = ConfigurationConstants.EnabledFeatureName;
                    }

                    break;
                case ConfigurationType.Quota:
                    {
                        stdout.WriteLine("\nEnter name of the component to which this key would be applicable for. e.g. environmentmanager, planmanager");
                        componentName = Console.ReadLine();
                        stdout.WriteLine("\nEnter name of the quota setting. e.g. max-plans-per-sub, max-environments-per-plan");
                        configurationName = Console.ReadLine();
                    }

                    break;

                case ConfigurationType.Setting:
                    {
                        stdout.WriteLine("\nEnter name of the component to which this key would be applicable for. e.g. capacitymanager, job-continuation-handler");
                        componentName = Console.ReadLine();
                        stdout.WriteLine("\nEnter name of the setting. e.g. enabled, enabled-resource-types");
                        configurationName = Console.ReadLine();
                    }

                    break;

                default: throw new InvalidOperationException();
            }

            if (string.IsNullOrEmpty(componentName) || string.IsNullOrEmpty(configurationName))
                throw new InvalidOperationException();

            return (componentName, configurationName);
        }

        private ConfigurationContext GetConfigurationContext(TextWriter stdout, TextWriter stderr)
        {
            stdout.WriteLine("\nSelect configuration context : \n1. Default \n2. Subscription \n3. Plane \n4. User");
            var type = Console.ReadLine();
            int choice = Convert.ToInt32(type);

            switch (choice)
            {
                case 1:
                    {
                        return ConfigurationContextBuilder.GetDefaultContext();
                    }
                    
                case 2:
                    {
                        stdout.WriteLine("\nEnter the subscription");
                        var sub = Console.ReadLine();
                        return ConfigurationContextBuilder.GetSubscriptionContext(sub);
                    }

                case 3:
                    {
                        stdout.WriteLine("\nEnter the subscription");
                        var sub = Console.ReadLine();
                        stdout.WriteLine("\nEnter the plan");
                        var plan = Console.ReadLine();
                        return ConfigurationContextBuilder.GetPlanContext(sub, plan);
                    }

                case 4:
                    {
                        stdout.WriteLine("\nEnter the user id");
                        var user = Console.ReadLine();
                        return ConfigurationContextBuilder.GetUserContext(user);
                    }

                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
