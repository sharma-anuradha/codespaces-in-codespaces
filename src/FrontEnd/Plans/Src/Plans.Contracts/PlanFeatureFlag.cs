// <copyright file="PlanFeatureFlag.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts
{
    /// <summary>
    /// Defines feature flags for plan.
    /// </summary>
    public enum PlanFeatureFlag
    {
        /// <summary>
        ///  Feature flag for vnet injection feature.
        /// </summary>
        VnetInjection = 1,
    }
}
