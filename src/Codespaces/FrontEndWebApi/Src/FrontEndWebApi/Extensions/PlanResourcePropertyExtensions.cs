// <copyright file="PlanResourcePropertyExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Extensions
{
    /// <summary>
    /// Extensions for plan properties.
    /// </summary>
    public static class PlanResourcePropertyExtensions
    {
        /// <summary>
        /// Build vsovnetproperty from vnet property.
        /// </summary>
        /// <param name="property">source.</param>
        /// <returns>result.</returns>
        public static VsoVnetProperties BuildVsoVnetProperty(this VnetProperties property)
        {
            if (property == default)
            {
                return default;
            }

            return new VsoVnetProperties()
            {
                SubnetId = property.SubnetId,
            };
        }

        /// <summary>
        /// Builds VsoPlanEncryptionProperties from PlanResourceEncryptionProperties.
        /// </summary>
        /// <param name="property">The source object.</param>
        /// <returns>The mapped object.</returns>
        public static VsoPlanEncryptionProperties BuildVsoEncryptionProperty(this PlanResourceEncryptionProperties property)
        {
            if (property == default)
            {
                return default;
            }

            return new VsoPlanEncryptionProperties
            {
                KeySource = property.KeySource,
                KeyVaultProperties = new VsoPlanKeyVaultProperties
                {
                    KeyName = property.KeyVaultProperties?.KeyName,
                    KeyVersion = property.KeyVaultProperties?.KeyVersion,
                    KeyVaultUri = property.KeyVaultProperties?.KeyVaultUri,
                },
            };
        }

        /// <summary>
        /// Updates the managed identity based on headers from a POST or PATCH.
        /// </summary>
        /// <param name="property">The source object.</param>
        /// <param name="headers">The headers from the incoming request.</param>
        /// <returns>The mapped object.</returns>
        public static VsoPlanIdentity BuildManagedIdentity(this PlanResourceIdentity property, PlanResourceHeaders headers)
        {
            if (string.IsNullOrEmpty(property?.Type))
            {
                return default;
            }

            return new VsoPlanIdentity
            {
                Type = property.Type,
                PrincipalId = headers.IdentityPrincipalId,
                IdentityUrl = headers.IdentityUrl,

                // note that one or both of the following headers will be present even without an MSI
                TenantId = headers.HomeTenantId ?? headers.ClientTenantId,
            };
        }
    }
}
