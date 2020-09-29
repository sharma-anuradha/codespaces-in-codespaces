// <copyright file="ConfigurationScopeGenerator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator
{
    /// <summary>
    /// A scope generator for various configurations.
    /// </summary>
    public class ConfigurationScopeGenerator : IConfigurationScopeGenerator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationScopeGenerator"/> class.
        /// </summary>
        /// <param name="options">The azure resource provider options.</param>
        /// <param name="controlPlaneInfo">control plane info.</param>
        public ConfigurationScopeGenerator(IOptions<ControlPlaneInfoOptions> options, IControlPlaneInfo controlPlaneInfo)
        {
            ControlPlaneSettings = Requires.NotNull(options?.Value?.ControlPlaneSettings, nameof(ControlPlaneSettings));
            ControlPlaneInfo = Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));

            ScopePriority = new List<ConfigurationScope>();

            // Scopes should be added in the order of inreasing priority.
            // Service(least) < Region < Subscription < plan < user (most) 

            // Scope with least priority
            ScopePriority.Add(ConfigurationScope.Service);
            ScopePriority.Add(ConfigurationScope.Region);
            ScopePriority.Add(ConfigurationScope.Subscription);
            ScopePriority.Add(ConfigurationScope.Plan);

            // Scope with highest priority
            ScopePriority.Add(ConfigurationScope.User);
        }

        // 1st element has the least priority and the last one has the highest priority.
        public IList<ConfigurationScope> ScopePriority { get; }

        private ControlPlaneSettings ControlPlaneSettings { get; }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        private string ServiceScopeConfiguration =>
            $"{ControlPlaneSettings.Prefix}";

        private string RegionScopeConfiguration =>
            $"{ServiceScopeConfiguration}-{ControlPlaneStampInfo.RegionCodes[ControlPlaneInfo.Stamp.Location]}";

        private string GlobalSubscriptionScopeConfiguration =>
            $"{ServiceScopeConfiguration}-subscription";

        private string RegionSubscriptionScopeConfiguration =>
            $"{RegionScopeConfiguration}-subscription";

        private string GlobalUserScopeConfiguration =>
            $"{ServiceScopeConfiguration}-user";

        private string RegionUserScopeConfiguration =>
            $"{RegionScopeConfiguration}-user";

        /// <inheritdoc/>
        public IEnumerable<string> GetScopes(ConfigurationContext context)
        {
            if (context == default)
            {
                context = ConfigurationContextBuilder.GetDefaultContext();
            }

            var scopeList = new List<string>();

            foreach (var scope in ScopePriority)
            {
                switch (scope)
                {
                    case ConfigurationScope.Service:
                        {
                            if (context.ServiceScopeApplicable)
                            {
                                scopeList.Add(ServiceScopeConfiguration);
                            }
                        }

                        break;
                    case ConfigurationScope.Region:
                        {
                            if (context.RegionScopeApplicable)
                            {
                                scopeList.Add(RegionScopeConfiguration);
                            }
                        }

                        break;
                    case ConfigurationScope.Subscription:
                        {
                            if (context.SubscriptionScopeApplicable)
                            {
                                scopeList.Add($"{GlobalSubscriptionScopeConfiguration}-{context.SubscriptionId}");
                                scopeList.Add($"{RegionSubscriptionScopeConfiguration}-{context.SubscriptionId}");
                            }
                        }

                        break;
                    case ConfigurationScope.Plan:
                        {
                            if (context.PlanScopeApplicable)
                            {
                                scopeList.Add($"{GlobalSubscriptionScopeConfiguration}-{context.SubscriptionId}-plan-{context.PlanId}");
                                scopeList.Add($"{RegionSubscriptionScopeConfiguration}-{context.SubscriptionId}-plan-{context.PlanId}");
                            }
                        }

                        break;
                    case ConfigurationScope.User:
                        {
                            if (context.UserScopeApplicable)
                            {
                                scopeList.Add($"{GlobalUserScopeConfiguration}-{context.UserId}");
                                scopeList.Add($"{RegionUserScopeConfiguration}-{context.UserId}");
                            }
                        }

                        break;
                    default: throw new ArgumentException(message: "invalid enum value", paramName: nameof(scope));
                }
            }

            // Reverse the list for consumption. The last one is the most specific scope and takes the highest priority.
            // Upon reversing the list, it becomes the first scope that should be looked up.
            scopeList.Reverse();
            return scopeList;
        }
    }
}