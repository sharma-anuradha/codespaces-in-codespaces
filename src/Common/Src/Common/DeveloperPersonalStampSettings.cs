// <copyright file="DeveloperPersonalStampSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Developer personal stamp settings.
    /// </summary>
    public class DeveloperPersonalStampSettings
    {
        /// <summary>
        /// Gets a value indicating whether the developer personal stamp should be set.
        /// </summary>
        public bool DeveloperStamp { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeveloperPersonalStampSettings"/> class.
        /// </summary>
        /// <param name="enable">True to enable developer personal stamp.</param>
        public DeveloperPersonalStampSettings(bool enable)
        {
            DeveloperStamp = enable;
        }
    }
}
