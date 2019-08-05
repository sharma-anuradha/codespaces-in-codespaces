// <copyright file="AllocateInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models
{
    public class AllocateInput
    {
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
