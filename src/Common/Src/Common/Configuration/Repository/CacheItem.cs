// <copyright file="CacheItem.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.Repository
{
    /// <summary>
    /// A class representing a cache item used in system configuration cache.
    /// </summary>
    public class CacheItem
    {
        public CacheItem(SystemConfigurationRecord value)
        {
            this.value = value;
            hitCount = 0;
        }

        private SystemConfigurationRecord value;

        private uint hitCount;

        public SystemConfigurationRecord GetValue()
        {
            ++hitCount;
            return value;
        }

        /// <summary>
        /// Gets the hit count.
        /// </summary>
        public uint GetHitCount()
        {
            return hitCount;
        }

        /// <summary>
        /// Sets the hit count.
        /// </summary>
        public void SetHitCount(uint newHitCount)
        {
            hitCount = newHitCount;
        }
    }
}
