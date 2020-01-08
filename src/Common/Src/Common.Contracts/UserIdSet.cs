// <copyright file="UserIdSet.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

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
        public UserIdSet(string canonicalUserId, string profileId, string profileProviderId)
        {
            CanonicalUserId = canonicalUserId;
            ProfileId = profileId;
            ProfileProviderId = profileProviderId;
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

            return id == CanonicalUserId || id == ProfileId || id == ProfileProviderId;
        }
    }
}
