// <copyright file="CrossRegionControlPlaneInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Caching;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <inheritdoc/>
    public class CrossRegionControlPlaneInfo : ICrossRegionControlPlaneInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CrossRegionControlPlaneInfo"/> class.
        /// </summary>
        /// <param name="controlPlaneAzureResourceAccessor">Control plane azure resource accessor.</param>
        /// <param name="controlPlaneInfo">Control plane info.</param>
        /// <param name="currentLocationProvider">Current location provider.</param>
        /// <param name="options">Control plane info options.</param>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        /// <param name="servicePrincipal">Azure service principal.</param>
        /// <param name="httpClient">Http client.</param>
        /// <param name="cache">Cache.</param>
        public CrossRegionControlPlaneInfo(
            IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor,
            IControlPlaneInfo controlPlaneInfo,
            ICurrentLocationProvider currentLocationProvider,
            IOptions<ControlPlaneInfoOptions> options,
            IResourceNameBuilder resourceNameBuilder,
            IServicePrincipal servicePrincipal,
            ControlPlaneAzureResourceAccessor.HttpClientWrapper httpClient,
            IManagedCache cache)
        {
            var currentLocations = new Dictionary<AzureLocation, ICurrentLocationProvider>();
            var currentControlPlaneInfo = new Dictionary<AzureLocation, IControlPlaneInfo>();
            var accessors = new Dictionary<AzureLocation, IControlPlaneAzureResourceAccessor>();

            foreach (var stamp in controlPlaneInfo.AllStamps.Values.Distinct())
            {
                var localCurrentLocationProvider = stamp.Location == currentLocationProvider.CurrentLocation ?
                    currentLocationProvider : new CurrentLocationProvider(stamp.Location);

                var localControlPlaneInfo = stamp.Location == currentLocationProvider.CurrentLocation ?
                    controlPlaneInfo : new ControlPlaneInfo(options, localCurrentLocationProvider, resourceNameBuilder);

                var localAccessor = stamp.Location == currentLocationProvider.CurrentLocation ?
                    controlPlaneAzureResourceAccessor : new ControlPlaneAzureResourceAccessor(localControlPlaneInfo, servicePrincipal, httpClient, cache);

                currentLocations.Add(stamp.Location, localCurrentLocationProvider);
                currentControlPlaneInfo.Add(stamp.Location, localControlPlaneInfo);
                accessors.Add(stamp.Location, localAccessor);
            }

            AllLocationProviders = new ReadOnlyDictionary<AzureLocation, ICurrentLocationProvider>(currentLocations);
            AllControlPlaneInfos = new ReadOnlyDictionary<AzureLocation, IControlPlaneInfo>(currentControlPlaneInfo);
            AllResourceAccessors = new ReadOnlyDictionary<AzureLocation, IControlPlaneAzureResourceAccessor>(accessors);
        }

        /// <inheritdoc/>
        public IReadOnlyDictionary<AzureLocation, ICurrentLocationProvider> AllLocationProviders { get; }

        /// <inheritdoc/>
        public IReadOnlyDictionary<AzureLocation, IControlPlaneInfo> AllControlPlaneInfos { get; }

        /// <inheritdoc/>
        public IReadOnlyDictionary<AzureLocation, IControlPlaneAzureResourceAccessor> AllResourceAccessors { get; }
    }
}
