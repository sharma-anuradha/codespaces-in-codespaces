// <copyright file="UpdateQuotaSettingCommand.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.IO;
using CommandLine;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil
{
    /// <summary>
    /// Update system configuration settings.
    /// </summary>
    [Verb("update-quota", HelpText = "Update system quota configuration settings.")]
    public class UpdateQuotaSettingCommand : SystemConfigurationCommandBase
    {
        /// <summary>
        /// Gets or sets a value indicating the plan id.
        /// </summary>
        [Option("plan", HelpText = "The Plan ID if setting the max environments for a plan.")]
        public string PlanId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the subscription id.
        /// </summary>
        [Option("subscription", HelpText = "The Subscription ID if setting the max plans for a subscription.")]
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the type of quota.
        /// </summary>
        [Option("quota", HelpText = "The type of quota. [global-max-plans, max-plans-per-sub, or max-environments-per-plan].", Required = true)]
        public string Quota { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the value of the setting.
        /// </summary>
        [Option("value", HelpText = "The quota value.", Required = true)]
        public string Value { get; set; }

        /// <inheritdoc/>
        protected override void ExecuteCommand(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            string id;

            switch (Quota)
            {
            case "max-plans-per-sub":
                if (string.IsNullOrEmpty(SubscriptionId))
                {
                    throw new Exception("The subscription id must be specified using --subscription");
                }

                if (!Guid.TryParse(SubscriptionId, out _))
                {
                    throw new Exception($"Invalid subscription id: {SubscriptionId}");
                }

                id = $"quota:max-plans-per-sub:{SubscriptionId}";
                break;
            case "max-environments-per-plan":
                if (string.IsNullOrEmpty(PlanId))
                {
                    throw new Exception("The plan id must be specified using --plan");
                }

                if (!Guid.TryParse(PlanId, out _))
                {
                    throw new Exception($"Invalid subscription id: {PlanId}");
                }

                id = $"quota:max-environments-per-plan:{PlanId}";
                break;
            case "global-max-plans":
                id = "quota:global-max-plans";
                break;
            default:
                throw new Exception($"Invalid quota type: {Quota}");
            }

            UpdateSystemConfigurationAsync(services, id, Value, stdout, stderr).Wait();
        }
    }
}
