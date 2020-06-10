// <copyright file="IIdentityMapEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.IdentityMap
{
    /// <summary>
    /// A two-way mapping between the v1 <see cref="ProfileId"/> and the v2 <see cref="CanonicalUserId"/>.
    /// </summary>
    public interface IIdentityMapEntity : ITaggedEntity, IEntity
    {
        /// <summary>
        /// Gets or sets the user name.
        /// </summary>
        string UserName { get; set; }

        /// <summary>
        /// Gets or sets the tenant id.
        /// </summary>
        string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the profile id.
        /// </summary>
        string ProfileId { get; set; }

        /// <summary>
        /// Gets or sets the profile provider id.
        /// </summary>
        string ProfileProviderId { get; set; }

        /// <summary>
        /// Gets or sets the canonical user id.
        /// </summary>
        string CanonicalUserId { get; set; }

        /// <summary>
        /// Gets or sets an array of user IDs that have been linked to the current identity
        /// through alternate sign-in flows.
        /// </summary>
        /// <remarks>
        /// Specifically, this enables linking different versions of MSA identities for
        /// the same account.
        ///
        /// This property is null for non-MSA identities or for MSA identities that have
        /// not yet had their links initialized.
        /// </remarks>
        string[] LinkedUserIds { get; set; }
    }
}
