// <copyright file="CapacitySettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Capacity
{
    /// <summary>
    /// Defines capacity settings.
    /// </summary>
    public class CapacitySettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CapacitySettings"/> class with default capacity settings.
        /// </summary>
        public CapacitySettings()
        {
            Min = 1;
            Max = 500;
            SpreadResourcesInGroups = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CapacitySettings"/> class.
        /// </summary>
        /// <param name="min">Minimum resource groups.</param>
        /// <param name="max">Maximum resource groups.</param>
        /// <param name="spread">True to spread in resource groups.</param>
        public CapacitySettings(int min, int max, bool spread)
        {
            Min = min;
            Max = max;
            SpreadResourcesInGroups = spread;
        }

        /// <summary>
        /// Gets the minimum number of resource groups.
        /// </summary>
        public int Min
        {
            get;
        }

        /// <summary>
        /// Gets the maximum number of resource groups.
        /// </summary>
        public int Max
        {
            get;
        }

        /// <summary>
        /// Gets a value indicating whether resources should be spread in resource groups.
        /// </summary>
        public bool SpreadResourcesInGroups
        {
            get;
        }

        /// <summary>
        /// Creates capacity settings for developer stamp.
        /// </summary>
        /// <returns>capacity settings object.</returns>
        public static CapacitySettings CreateDeveloperCapacitySettings()
        {
            return new CapacitySettings(1, 1, false);
        }
    }
}
