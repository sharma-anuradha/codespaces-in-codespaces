// <copyright file="PrivacyCommandFeedSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PCFAgent
{
    /// <summary>
    /// Pcf Endpoints.
    /// </summary>
    public enum PcfEndpoint
    {
        /// <summary>
        /// Pcf PPE.
        /// To be used with VSO Dev.
        /// </summary>
        Ppe = 0,

        /// <summary>
        /// Pcf Production.
        /// To be used with VSO PPE and Production.
        /// </summary>
        Prod = 1,
    }

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

        /// <summary>
        /// Gets or sets Pcf endpoint.
        /// </summary>
        public PcfEndpoint PcfEndpoint { get; set; }
    }
}
