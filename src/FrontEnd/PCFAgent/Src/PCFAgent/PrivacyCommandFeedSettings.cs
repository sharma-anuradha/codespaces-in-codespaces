// <copyright file="PrivacyCommandFeedSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PCFAgent
{
    /// <summary>
    /// PrivacyCommandFeed Settings.
    /// </summary>
    public class PrivacyCommandFeedSettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether PCF Agent is enabled.
        /// </summary>
        public bool IsPcfEnabled { get; set; }

        /// <summary>
        /// Gets or sets PCF Agent id.
        /// </summary>
        public Guid PcfAgentId { get; set; }
    }
}
