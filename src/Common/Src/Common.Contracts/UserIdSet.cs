// <copyright file="UserIdSet.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Linq;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// A tuple of user identity values.
    /// </summary>
    public class UserIdSet
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Contracts.UserIdSet"/> class.
        /// </summary>
        /// <param name="canonicalUserId">The canonical user id.</param>
        /// <param name="profileId">The profile id.</param>
        /// <param name="profileProviderId">The profile provider id.</param>
        /// <param name="linkedUserIds">Optional array of linked user IDs. See
        /// <see cref="LinkedUserIds"/>.</param>
        public UserIdSet(
            string canonicalUserId,
            string profileId,
            string profileProviderId,
            string[] linkedUserIds = null)
        {
            CanonicalUserId = canonicalUserId;
            ProfileId = profileId;
            ProfileProviderId = profileProviderId;
            LinkedUserIds = linkedUserIds;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Contracts.UserIdSet"/> class.
        /// </summary>
        /// <param name="ownerId">An existing user/owner id.</param>
        public UserIdSet(string ownerId)
        {
            CanonicalUserId = ownerId;
        }

        /// <summary>
        /// Gets the canonical user id.
        /// </summary>
        public string CanonicalUserId { get; }

        /// <summary>
        /// Gets the profile id.
        /// </summary>
        public string ProfileId { get; }

        /// <summary>
        /// Gets the profile provider id.
        /// </summary>
        public string ProfileProviderId { get; }

        /// <summary>
        /// Gets an array of user IDs that have been linked to the current identity
        /// through alternate sign-in flows.
        /// </summary>
        /// <remarks>
        /// Specifically, this enables linking different versions of MSA identities for
        /// the same account.
        ///
        /// This property is null for non-MSA identities or for MSA identities that have
        /// not yet had their links initialized.
        /// </remarks>
        public string[] LinkedUserIds { get; }

        /// <summary>
        /// Gets either the canonical user id (preferred) or else the profile id.
        /// </summary>
        public string PreferredUserId => CanonicalUserId ?? ProfileId;

        /// <summary>
        /// Test if <paramref name="id"/> is equal to any ids in the set.
        /// </summary>
        /// <param name="id">The id to test.</param>
        /// <returns>True if any match.</returns>
        public bool EqualsAny(string id)
        {
            if (id is null)
            {
                return false;
            }

            return id == CanonicalUserId || id == ProfileId || id == ProfileProviderId ||
                LinkedUserIds?.Contains(id) == true;
        }
    }
}
