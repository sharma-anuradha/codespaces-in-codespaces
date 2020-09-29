// <copyright file="ConfigurationContext.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration
{
    /// <summary>
    /// Class to define the scope of a configuration.
    /// </summary>
    public class ConfigurationContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationContext"/> class.
        /// </summary>
        public ConfigurationContext()
        {
        }

        public bool ServiceScopeApplicable { get; set; } = true;

        public bool RegionScopeApplicable { get; set; } = true;

        public string SubscriptionId { get; set; } = string.Empty;

        public string PlanId { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;

        public bool SubscriptionScopeApplicable
        {
            get
            {
                return !string.IsNullOrEmpty(SubscriptionId);
            }
        }

        public bool PlanScopeApplicable
        {
            get
            {
                return (!string.IsNullOrEmpty(SubscriptionId)) && (!string.IsNullOrEmpty(PlanId));
            }
        }

        public bool UserScopeApplicable
        {
            get
            {
                return !string.IsNullOrEmpty(UserId);
            }
        }
    }
}
