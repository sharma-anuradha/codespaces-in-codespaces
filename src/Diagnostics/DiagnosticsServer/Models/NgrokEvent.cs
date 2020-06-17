// <copyright file="NgrokEvent.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.DiagnosticsServer.Models
{
    /// <summary>
    /// SignalR Ngrok Event.
    /// </summary>
    public class NgrokEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NgrokEvent"/> class.
        /// </summary>
        /// <param name="type">Type of Event.</param>
        /// <param name="name">Name of Event.</param>
        public NgrokEvent(EventType type, string name)
        {
            this.Event = new { };
            this.EventName = name;
            this.EventType = type;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NgrokEvent"/> class.
        /// </summary>
        /// <param name="type">Type of Event.</param>
        /// <param name="name">Name of Event.</param>
        /// <param name="ngrokEvent">Object to be returned representing the event.</param>
        public NgrokEvent(EventType type, string name, dynamic ngrokEvent)
        {
            this.Event = ngrokEvent;
            this.EventName = name;
            this.EventType = type;
        }

        /// <summary>
        /// Gets or sets the type of event being returned.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "type")]
        public EventType EventType { get; set; }

        /// <summary>
        /// Gets or sets the event name.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "name")]
        public string EventName { get; set; }

        /// <summary>
        /// Gets or sets the event.
        /// </summary>
        [JsonProperty(PropertyName = "event")]
        public dynamic Event { get; set; }
    }
}
