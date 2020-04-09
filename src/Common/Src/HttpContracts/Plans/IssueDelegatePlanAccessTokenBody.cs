// <copyright file="IssueDelegatePlanAccessTokenBody.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Plans
{
    /// <summary>
    /// The request body for delegate plan access token API requests.
    /// </summary>
    public class IssueDelegatePlanAccessTokenBody
    {
        /// <summary>
        /// Gets or sets the identity of the user the delegate token will be given to.
        /// </summary>
        public DelegateIdentity Identity { get; set; }

        /// <summary>
        /// Gets or sets the space delimited requested scopes of the delegate token.
        /// </summary>
        public string Scope { get; set; }

        /// <summary>
        /// Gets or sets the requested expiration of the delegate token.
        /// </summary>
        [JsonConverter(typeof(UnixDateTimeConverter))]
        public DateTime? Expiration { get; set; }

        /// <summary>
        /// Gets or sets the list of environment IDs that the delegate token will be scoped to,
        /// or null if the token is plan-scoped and not environment-scoped.
        /// </summary>
        public string[] EnvironmentIds { get; set; }
    }
}
