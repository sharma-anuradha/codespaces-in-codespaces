// <copyright file="EnvironmentPool.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    public class EnvironmentPool
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the Target Count that this resource should be maintained at when pooled.
        /// </summary>
        public int TargetCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the pool is enabled.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gets the Max Create Batch Count.
        /// </summary>
        public int MaxCreateBatchCount
        {
            get { return 25; }
        }

        /// <summary>
        /// Gets the Max Delete Batch Count.
        /// </summary>
        public int MaxDeleteBatchCount
        {
            get { return 35; }
        }

        /// <summary>
        /// Gets or sets the additional resource details that identiy the resource.
        /// </summary>
        public EnvironmentPoolDetails Details { get; set; }
    }
}