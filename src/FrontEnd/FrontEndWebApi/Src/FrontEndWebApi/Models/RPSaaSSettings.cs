// <copyright file="RPSaaSSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models
{
    /// <summary>
    /// Config settings related to RPSaaS authentication.
    /// </summary>
    public class RPSaaSSettings
    {
        /// <summary>
        /// Gets or sets the RPSaaS ApplicationID.
        /// This appid claim will be present for all api calls coming from RPSaaS.
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// Gets or sets the Authority URL used to valided RPSaaS signing signature.
        /// </summary>
        public string Authority { get; set; }

        /// <summary>
        /// Gets or sets the Issuer hostname for the RPSaaS iss claim.
        /// </summary>
        public string IssuerHostname { get; set; }

        /// <summary>
        /// Gets or sets the Url where the ARM signed user token public cert can be fetched from.
        /// </summary>
        public string SignedUserTokenCertUrl { get; set; }
    }
}
