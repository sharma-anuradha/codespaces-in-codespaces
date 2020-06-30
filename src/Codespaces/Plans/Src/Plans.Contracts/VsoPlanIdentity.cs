// <copyright file="VsoPlanIdentity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts
{
    /// <summary>
    /// The managed identity associated with this plan.
    /// </summary>
    public class VsoPlanIdentity
    {
        /// <summary>
        /// Gets or sets the type of identity, such as "SystemAssigned".
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the service principal ID of the managed identity.
        /// </summary>
        public Guid? PrincipalId { get; set; }

        /// <summary>
        /// Gets or sets the tenant ID of the managed identity.
        /// </summary>
        public Guid? TenantId { get; set; }

        /// <summary>
        /// Gets or sets the URL of the managed identity endpoint for this service principal.
        /// </summary>
        public string IdentityUrl { get; set; }

        public static bool operator ==(VsoPlanIdentity a, VsoPlanIdentity b)
        {
            return a is null ? b is null : a.Equals(b);
        }

        public static bool operator !=(VsoPlanIdentity a, VsoPlanIdentity b)
        {
            return !(a == b);
        }

        /// <summary>Tests if this managed identity equals another.</summary>
        /// <param name="other">Another managed identity object.</param>
        /// <returns>True if all managed identity properties are equal.</returns>
        public bool Equals(VsoPlanIdentity other)
        {
            return other != default &&
                other.Type == Type &&
                other.PrincipalId == PrincipalId &&
                other.TenantId == TenantId &&
                other.IdentityUrl == IdentityUrl;
        }

        /// <summary>Tests if this managed identity equals another.</summary>
        /// <param name="obj">Another managed identity object.</param>
        /// <returns>True if all managed identity properties are equal.</returns>
        public override bool Equals(object obj) => Equals(obj as VsoPlanIdentity);

        /// <summary>Gets a hashcode for the managed identity.</summary>
        /// <returns>Hash code derived from each of the managed identity properties.</returns>
        public override int GetHashCode()
        {
            return (Type?.GetHashCode() ?? 0) ^
                (PrincipalId?.GetHashCode() ?? 0) ^
                (TenantId?.GetHashCode() ?? 0) ^
                (IdentityUrl?.GetHashCode() ?? 0);
        }
    }
}
