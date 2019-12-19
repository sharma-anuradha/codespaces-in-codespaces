// <copyright file="MeResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts
{
    /// <summary>
    /// The plan REST API result.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class MeResult
    {
        /// <summary>
        /// Gets or sets the avatar uri.
        /// </summary>
        public string AvatarUri { get; set; }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the email.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the preferred user id.
        /// </summary>
        public string PreferredUserId { get; set; }

        /// <summary>
        /// Gets or sets the canonical user id.
        /// </summary>
        public string CanonicalUserId { get; set; }

        /// <summary>
        /// Gets or sets the profile id.
        /// </summary>
        public string ProfileId { get; set; }

        /// <summary>
        /// Gets or sets the profile provider id.
        /// </summary>
        public string ProfileProviderId { get; set; }

        /// <summary>
        /// Gets or sets the id map key for the user's email/tenant combo.
        /// </summary>
        public string IdMapKey { get; set; }
    }
}
