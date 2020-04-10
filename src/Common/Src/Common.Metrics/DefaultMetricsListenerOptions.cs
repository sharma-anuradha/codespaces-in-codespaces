// <copyright file="DefaultMetricsListenerOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Metrics
{
    /// <summary>
    /// The metrics listener options.
    /// </summary>
    public class DefaultMetricsListenerOptions
    {
        /// <summary>
        /// Gets or sets the MDSD event source which is parsed from the <see cref="LoggingConstants.Service"/> logging property.
        /// </summary>
        /// <remarks>
        /// The underlying diagnostics logger uses the "service" property to route logging events. This property
        /// is used to cause metrics events to be routed to different storage from normal telemetry logging events.
        /// </remarks>
        public string MdsdEventSource { get; set; }
    }
}
