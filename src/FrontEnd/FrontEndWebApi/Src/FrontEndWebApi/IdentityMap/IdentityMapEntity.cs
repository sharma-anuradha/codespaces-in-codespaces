// <copyright file="IdentityMapEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.VsSaaS.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.IdentityMap
{
    /// <inheritdoc/>
    public class IdentityMapEntity : TaggedEntity, IIdentityMapEntity
    {
        private static readonly Regex BadCosmosDbIdChars = new Regex(@"[/\\?#]");
        private static readonly Dictionary<char, char> BadCosmosDbIdCharMap = new Dictionary<char, char>
        {
            { '/', '\u2044' },   // fraction slash ⁄
            { '\\', '\u2215' },  // division slash ∕
            { '?', '\u2202' },   // partial differential ∂
            { '#', '\u20bc' },   // undefined ₼
        };

        private string userName;
        private string tenantId;

        /// <summary>
        /// Initializes a new instance of the <see cref="IdentityMapEntity"/> class.
        /// </summary>
        public IdentityMapEntity()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IdentityMapEntity"/> class.
        /// </summary>
        /// <param name="userName">The preferred username.</param>
        /// <param name="tenantId">The tenant id.</param>
        public IdentityMapEntity(string userName, string tenantId)
        {
            UserName = userName;
            TenantId = tenantId;
        }

        /// <inheritdoc/>
        public string UserName
        {
            get => userName;
            set
            {
                userName = value;
                SetId();
            }
        }

        /// <inheritdoc/>
        public string TenantId
        {
            get => tenantId;
            set
            {
                tenantId = value;
                SetId();
            }
        }

        /// <inheritdoc/>
        public string ProfileId { get; set; }

        /// <inheritdoc/>
        public string ProfileProviderId { get; set; }

        /// <inheritdoc/>
        public string CanonicalUserId { get; set; }

        /// <summary>
        /// Make a composite id from the user name and tenant id.
        /// The ID may alter the <paramref name="userName"/> portion to
        /// ensure that it is a valid CosmosDB id.
        /// </summary>
        /// <param name="userName">The user name.</param>
        /// <param name="tenantId">The tenant id.</param>
        /// <returns>The id.</returns>
        public static string MakeId(string userName, string tenantId)
        {
            if (!string.IsNullOrEmpty(userName))
            {
                // If there are any illegal characters,
                // replace them valid characters that are unlikely to be used in
                // a like email address.
                for (var match = BadCosmosDbIdChars.Match(userName);
                     match.Success;
                     match = BadCosmosDbIdChars.Match(userName))
                {
                    var badChar = match.Value[0];
                    if (!BadCosmosDbIdCharMap.TryGetValue(badChar, out var goodChar))
                    {
                        // This is unexpected. The character ought to have been mapped.
                        throw new NotSupportedException($"The characters {badChar} is not supported in the user name.");
                    }

                    userName = userName.Replace(badChar, goodChar);
                }
            }

            return string.Concat(userName, ":", tenantId);
        }

        private void SetId() => Id = MakeId(UserName, TenantId);
    }
}
