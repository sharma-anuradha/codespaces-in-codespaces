// <copyright file="ResourceRecord.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class ResourceRecord : TaggedEntity
    { 
        /// <summary>
        /// 
        /// </summary>
        public string ResourceId { get; set; }

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

        /// <summary>
        /// 
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool IsAssigned { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime Assigned { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public dynamic Properties { get; set; }
    }
}
