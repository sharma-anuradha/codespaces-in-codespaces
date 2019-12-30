// <copyright file="SubscriptionStates.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// See https://github.com/Azure/azure-resource-manager-rpc/blob/master/v1.0/subscription-lifecycle-api-reference.md#subscription-states.
    /// </summary>
    public static class SubscriptionStates
    {
        /// <summary>Initial and terminal state. </summary>
        public const string Unregistered = "unregistered";

        public const string Registered = "registered";
        public const string Warned = "warned";
        public const string Suspended = "suspended";
        public const string Deleted = "deleted";
    }
}
