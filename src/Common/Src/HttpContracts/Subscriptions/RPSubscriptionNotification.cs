// <copyright file="RPSubscriptionNotification.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Subscriptions
{
    /// <summary>
    /// JSON body properties for subscription notifications.
    /// </summary>
    public class RPSubscriptionNotification
    {
        /// <summary>
        /// Gets or sets the state.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "state")]
        public string State { get; set; }

        /// <summary>
        /// Gets or sets the registration date.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "registrationDate")]
        public DateTime RegistrationDate { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="RPSubscriptionProperties"/> properties.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "properties")]
        public RPSubscriptionProperties Properties { get; set; }
    }
}
