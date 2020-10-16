// <copyright file="ConfigurationContextBuilder.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration
{
    /// <summary>
    /// Configuration context builder helper.
    /// </summary>
    public static class ConfigurationContextBuilder
    {
        public static ConfigurationContext GetDefaultContext()
        {
            return new ConfigurationContext();
        }

        public static ConfigurationContext GetSubscriptionContext(string subscription)
        {
            var context = new ConfigurationContext();
            context.SubscriptionId = subscription;
            return context;
        }

        public static ConfigurationContext GetPlanContext(string subscription, string plan)
        {
            var context = new ConfigurationContext();
            context.SubscriptionId = subscription;
            context.PlanId = plan;
            return context;
        }

        public static ConfigurationContext GetUserContext(string user)
        {
            var context = new ConfigurationContext();
            context.UserId = user;
            return context;
        }
    }
}
