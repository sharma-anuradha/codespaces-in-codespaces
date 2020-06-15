// <copyright file="ResourceProxyComputeDetails.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models
{
    /// <summary>
    /// Resource Compute Details.
    /// </summary>
    public class ResourceProxyComputeDetails : ResourceProxyDetails
    {
        private const string ComputeOSName = "computeOS";

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceProxyComputeDetails"/> class.
        /// </summary>
        /// <param name="record">Target record.</param>
        public ResourceProxyComputeDetails(ResourceRecord record)
            : base(record)
        {
        }

        /// <summary>
        /// Gets the underling image name.
        /// </summary>
        public ComputeOS ComputeOS
        {
            get
            {
                return (ComputeOS)Enum.Parse(typeof(ComputeOS), Record.PoolReference.Dimensions.GetValueOrDefault(ComputeOSName), true);
            }
        }

        /// <summary>
        /// Gets the OS disk record if exists.
        /// </summary>
        public string OSDiskRecordId
        {
            get
            {
                return Record
                    .Components?
                    .Items?
                    .Values
                    .SingleOrDefault(x => x.ComponentType == ResourceType.OSDisk)?
                    .ResourceRecordId;
            }
        }
    }
}
