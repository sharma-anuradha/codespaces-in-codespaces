// <copyright file="BannedReason.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions
{
    /// <summary>
    /// The reason a subscription is marked banned.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum BannedReason
    {
        /// <summary>
        /// Other or unknown reason.
        /// </summary>
        Other = 0,

        /// <summary>
        /// The VMs are suspected of DDOS attacks.
        /// </summary>
        SuspectedDDOS = 1,

        /// <summary>
        /// The subscription is suspected of fraudulent activity.
        /// </summary>
        SuspectedFraud = 2,
    }
}