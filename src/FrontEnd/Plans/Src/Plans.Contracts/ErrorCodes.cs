﻿// <copyright file="ErrorCodes.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts
{
    /// <summary>
    /// Lists of codes returned from <see cref="IPlanManager"/>.
    /// </summary>
    public enum ErrorCodes
    {
        /// <summary>
        /// Unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Quota exceeded.
        /// </summary>
        ExceededQuota = 1,

        /// <summary>
        /// VsoPlan does not exist.
        /// </summary>
        PlanDoesNotExist = 2,

        /// <summary>
        /// Azure subscription has been marked as banned.
        /// </summary>
        SubscriptionBanned = 3,

        /// <summary>
        /// Azure subscription state is not 'Registered'.
        /// </summary>
        SubscriptionStateNotRegistered = 4,

        /// <summary>
        /// Feature is disabled.
        /// </summary>
        FeatureDisabled = 5,
    }
}
