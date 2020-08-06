// <copyright file="ScheduledTaskHelpers.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Tasks
{
    /// <summary>
    /// Helper methods for scheduled tasks.
    /// </summary>
    public static class ScheduledTaskHelpers
    {
        /// <summary>
        /// Gets the id shards for scheduled tasks.
        /// </summary>
        /// <returns>List of shards.</returns>
        public static IEnumerable<string> GetIdShards()
        {
            // Basic shard by starting resource id character
            // NOTE: If over time we needed an additional dimention, we could add region
            //       and do a cross product with it.
            return new List<string> { "a", "b", "c", "d", "e", "f", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" }.Shuffle();
        }
    }
}
