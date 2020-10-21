// <copyright file="EnableSubscriptionSettingCommand.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil
{
    /// <summary>
    /// Update database settings to enable subscription.
    /// </summary>
    [Verb("enable-subscription", HelpText = "Update database settings to enable subscription.")]
    public class EnableSubscriptionSettingCommand : SystemConfigurationCommandBase
    {
        /// <summary>
        /// Look up table to convert Location to region.
        /// </summary>
        private static readonly Dictionary<AzureLocation, string> RegionCodes = new Dictionary<AzureLocation, string>
        {
            { AzureLocation.EastUs, "use" },
            { AzureLocation.SouthEastAsia, "asse" },
            { AzureLocation.WestEurope, "euw" },
            { AzureLocation.WestUs2, "usw2" },
            { AzureLocation.EastUs2Euap, "usec" },
        };

        /// <summary>
        /// Gets or sets a value indicating the subscription id.
        /// </summary>
        [Option("subscription", HelpText = "The Subscription ID.", Required = true)]
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether subscription should be disabled.
        /// </summary>
        [Option("disable", HelpText = "If provided subscription should be disabled otherwise enabled.", SetName = "Disable")]
        public bool Disable { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the subscription enable setting should be removed.
        /// </summary>
        [Option("remove", HelpText = "Remove the subscription enabled settings on the database.", SetName = "Remove")]
        public bool Remove { get; set; }

        /// <inheritdoc/>
        public override bool UseBackEnd => true;

        /// <inheritdoc/>
        protected override void ExecuteCommand(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            string value = Remove ? null : (Disable ? "false" : "true");

            var azureSubscriptionCatalog = services.GetRequiredService<IAzureSubscriptionCatalog>();
            var subscriptions = azureSubscriptionCatalog.AzureSubscriptions.ToList();
            var subscription = subscriptions.SingleOrDefault(s => s.SubscriptionId == SubscriptionId);
            if (subscription == null)
            {
                throw new InvalidOperationException("Subscription '{SubscriptionId}' was not found.");
            }
            
            foreach (var location in subscription.Locations)
            {
                var region = RegionCodes[location];
                UpdateSystemConfigurationAsync(services, $"setting:vsclk-{region}:capacitymanager-subscription-enabled-{SubscriptionId}", value, stdout, stderr).Wait();
            }
        }
    }
}
