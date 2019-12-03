// <copyright file="TelemetryData.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.VsoAgent
{
    /// <summary>
    /// TelemetryData from Vso Agent.
    /// </summary>
    public class TelemetryData
    {
        /// <summary>
        /// Gets or sets the time at which the telemetry entry is logged by Vso Agent.
        /// </summary>
        public string Time { get; set; }

        /// <summary>
        /// Gets or sets the telemetry log message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the log level.
        /// </summary>
        public string Level { get; set; }

        /// <summary>
        /// Gets or sets the log values.
        /// </summary>
        public Dictionary<string, string> OptionalValues { get; set; }
    }
}
