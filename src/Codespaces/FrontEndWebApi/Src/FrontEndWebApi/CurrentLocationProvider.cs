// <copyright file="CurrentLocationProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi
{
    /// <summary>
    /// Provides the current Azure location of the running service.
    /// </summary>
    public class CurrentLocationProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CurrentLocationProvider"/> class.
        /// </summary>
        /// <param name="location">The location to use as the current location.</param>
        public CurrentLocationProvider(AzureLocation location)
        {
            CurrentLocation = location;
        }

        /// <summary>
        /// Gets the current location where the service is executing.
        /// </summary>
        public AzureLocation CurrentLocation { get; }
    }
}
