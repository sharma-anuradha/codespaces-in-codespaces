// <copyright file="EnvironmentStates.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    public static class EnvironmentStates
    {
        // TODO: Update states based on actual state transitions driven by environment management service.

        /// <summary>Initial state.</summary>
        public const string Created = "created";

        public const string Provisioning = "provisioning";
        public const string Resuming = "resuming";
        public const string Running = "running";
        public const string Connected = "connected";
        public const string Suspending = "suspending";
        public const string Suspended = "suspended";

        /// <summary>Terminal state.</summary>
        public const string Deleted = "deleted";
    }
}
