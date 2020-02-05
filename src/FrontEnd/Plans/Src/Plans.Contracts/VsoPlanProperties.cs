﻿// <copyright file="VsoPlanProperties.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Plans
{
    /// <summary>
    /// Database entry that defines the plan settings.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class VsoPlanProperties
    {
        /// <summary>
        /// Gets or sets the default auto suspend timeout in minutes to be applied to environments in this plan.
        /// </summary>
        public int? DefaultAutoSuspendDelayMinutes { get; set; }

        /// <summary>
        /// Gets or sets the default sku name to use for creating environments in this plan.
        /// </summary>
        public string DefaultEnvironmentSku { get; set; }

        public static bool operator ==(VsoPlanProperties a, VsoPlanProperties b) =>
           a is null ? b is null : a.Equals(b);

        public static bool operator !=(VsoPlanProperties a, VsoPlanProperties b) => !(a == b);

        /// <summary> Tests if this plan settings equals another plan settings.</summary>
        /// <param name="other">Another plan settings object.</param>
        /// <returns>True if all plan properties are equal.</returns>
        public bool Equals(VsoPlanProperties other)
        {
            return other != null &&
                other.DefaultAutoSuspendDelayMinutes == DefaultAutoSuspendDelayMinutes &&
                other.DefaultEnvironmentSku == DefaultEnvironmentSku;
        }

        /// <summary> Tests if this plan settings equals another plan settings.</summary>
        /// <param name="obj">Another plan settings object.</param>
        /// <returns>True if all plan properties are equal.</returns>
        public override bool Equals(object obj) => Equals(obj as VsoPlanProperties);

        /// <summary>Gets a hashcode for the plan settings.</summary>
        /// <returns>Hash code derived from each of the plan settings properties.</returns>
        public override int GetHashCode() => DefaultAutoSuspendDelayMinutes.GetHashCode() ^
                                                (DefaultEnvironmentSku?.GetHashCode() ?? 0);
    }
}