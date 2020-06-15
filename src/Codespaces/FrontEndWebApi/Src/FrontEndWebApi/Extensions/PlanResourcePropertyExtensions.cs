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
    }
}
