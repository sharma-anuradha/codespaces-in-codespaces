// <copyright file="RPaaSSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models
{
    /// <summary>
    /// Config settings related to RPaaS authentication.
    /// </summary>
    public class RPaaSSettings
    {
        /// <summary>
        /// Gets or sets the RPaaS ApplicationID.
        /// This appid claim will be present for all api calls coming from RPaaS.
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// Gets or sets the Authority URL used to valided RPaaS signing signature.
        /// </summary>
        public string Authority { get; set; }

        /// <summary>
        /// Gets or sets the Issuer hostname for the RPaaS iss claim.
        /// </summary>
        public string IssuerHostname { get; set; }

        /// <summary>
        /// Gets or sets the Url where the ARM signed user token public cert can be fetched from.
        /// </summary>
        public string SignedUserTokenCertUrl { get; set; }

        /// <summary>
        /// Gets or sets the Url where registered subscription information can be found.
        /// </summary>
        public string RegisteredSubscriptionsUrl { get; set; }
    }
}
