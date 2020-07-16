// <copyright file="Profile.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Represents a Live Share user profile.
    /// </summary>
    public class Profile
    {
        /// <summary>
        /// Gets or sets the user's id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the user's email.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the user name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the user's avatar.
        /// </summary>
        public string AvatarUri { get; set; }

        /// <summary>
        /// Gets or sets the full user name.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the authentication provider.
        /// </summary>
        public string Provider { get; set; }

        /// <summary>
        /// Gets or sets the authentication provider id.
        /// </summary>
        public string ProviderId { get; set; }

        /// <summary>
        /// Gets or sets the user's status.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user is anonymous.
        /// </summary>
        public bool IsAnonymous { get; set; } = false;

        /// <summary>
        /// Gets or sets the dictionary of programs that the user is enrolled in.
        /// </summary>
        public Dictionary<string, object> Programs { get; set; }
    }
}
