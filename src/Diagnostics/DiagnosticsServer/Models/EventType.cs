// <copyright file="EventType.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.DiagnosticsServer.Models
{
    /// <summary>
    /// Event types for Ngrok Events.
    /// </summary>
    public enum EventType
    {
        /// <summary>
        /// Empty Event.
        /// </summary>
        Empty,

        /// <summary>
        /// Error Event.
        /// Used if we hit an error.
        /// </summary>
        Error,

        /// <summary>
        /// Info Event.
        /// When we have information.
        /// </summary>
        Info,
    }
}
