// <copyright file="SubscriptionStateEnum.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// class.
    /// </summary>
    public enum SubscriptionStateEnum
    {
        /// <summary>
        /// Registered.
        /// All management APIs must function (PUT/PATCH/DELETE/POST/GET)
        /// </summary>
        Registered = 0,

        /// <summary>
        /// Unregistered.
        /// No op. All resources would have already been removed.
        /// </summary>
        Unregistered,

        /// <summary>
        /// Warned.
        /// GET/DELETE management APIs must function.
        /// PUT/PATCH/POST must not.
        /// </summary>
        Warned,

        /// <summary>
        /// Suspended.
        /// GET/DELETE management APIs must function.
        /// PUT/PATCH/POST must not.
        /// </summary>
        Suspended,

        /// <summary>
        /// Deleted.
        /// Remove all resources.
        /// </summary>
        Deleted,
    }
}
