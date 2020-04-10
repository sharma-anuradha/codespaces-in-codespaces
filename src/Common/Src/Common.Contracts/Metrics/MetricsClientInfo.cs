// <copyright file="MetricsClientInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Metrics
{
    /// <summary>
    /// Metrics information for an external HTTP client that has invoked the VSO service.
    /// </summary>
    public class MetricsClientInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetricsClientInfo"/> class.
        /// </summary>
        /// <param name="isoCountryCode">The ISO 2-letter country code.</param>
        /// <param name="vsoClientType">The VSO client type.</param>
        public MetricsClientInfo(string isoCountryCode, VsoClientType? vsoClientType)
        {
            IsoCountryCode = isoCountryCode;
            AzureGeography = MetricsUtilities.IsoCountryCodeToAzurePublicGeography(isoCountryCode);
            VsoClientType = vsoClientType;
        }

        /// <summary>
        /// Gets or set the ISO 2-letter country code.
        /// </summary>
        public string IsoCountryCode { get; }

        /// <summary>
        /// Gets the Aure geography.
        /// </summary>
        public AzureGeography? AzureGeography { get; }

        /// <summary>
        /// Gets the VSO client type.
        /// </summary>
        public VsoClientType? VsoClientType { get; }
    }
}
