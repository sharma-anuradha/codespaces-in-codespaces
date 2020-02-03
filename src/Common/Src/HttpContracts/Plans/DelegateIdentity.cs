// <copyright file="DelegateIdentity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Plans
{
    /// <summary>
    /// The identity metadata for the target user a delegated plan access token.
    /// </summary>
    public class DelegateIdentity
    {
        /// <summary>
        /// Gets or sets the Id which is the unique identitifier which should never change.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string DisplayName { get; set; }
    }
}
