// <copyright file="OutOfCapacityException.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class OutOfCapacityException : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        public OutOfCapacityException(string skuName, ResourceType type, string location)
            : base("Pool is currently empty")
        {
            SkuName = skuName;
            Type = type;
            Location = location;
        }

        /// <summary>
        /// 
        /// </summary>
        public string SkuName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ResourceType Type { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Location { get; set; }
    }
}
