// <copyright file="IIdentityMapEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.IdentityMap
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
    }
}
