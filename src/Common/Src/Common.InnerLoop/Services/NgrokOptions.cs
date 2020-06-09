// <copyright file="NgrokOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore.Services
{
    /// <summary>
    /// Options for setting up Ngrok.
    /// </summary>
    public class NgrokOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether to disable all Ngrok integration features.
        /// </summary>
        public bool Disable { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to automatically detect URL from <see cref="Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature"/>.
        /// </summary>
        public bool DetectUrl { get; set; } = true;

        /// <summary>
        /// Gets or sets a value to set the local URL Ngrok will proxy to. Must be http (not https) at this time.
        /// Implies <see cref="DetectUrl"/> is false.
        /// </summary>
        public string ApplicationHttpUrl { get; set; } = null;

        /// <summary>
        /// Gets or sets a path to the Ngrok executable. If not set, the execution directory and PATH will be searched.
        /// Implies <see cref="ManageNgrokProcess"/> is true.
        /// </summary>
        public string NgrokPath { get; set; } = null;

        /// <summary>
        /// Gets or sets a time in milliseconds to wait for the ngrok process to start.
        /// Implies <see cref="ManageNgrokProcess"/> is true.
        /// Default is 5 seconds.
        /// </summary>
        public int ProcessStartTimeoutMs { get; set; } = 5000;

        /// <summary>
        /// Gets or sets a value indicating whether to redirect Ngrok process logs to Microsoft.Extensions.Logging.
        /// Implies <see cref="ManageNgrokProcess"/> is true.
        /// </summary>
        public bool RedirectLogs { get; set; } = true;
    }
}
