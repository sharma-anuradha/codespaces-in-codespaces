// <copyright file="BillingJobConstants.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Constants pertaining to the orchestration of jobs.
    /// </summary>
    public static class BillingJobConstants
    {
        /// <summary>
        /// The number of concurrent consumers.
        /// </summary>
        public const int ConcurrentConsumerCount = 64;
    }
}
