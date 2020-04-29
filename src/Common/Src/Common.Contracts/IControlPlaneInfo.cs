// <copyright file="IControlPlaneInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// The control-plane information.
    /// </summary>
    public interface IControlPlaneInfo
    {
        /// <summary>
        /// Gets the control-plane enivronment resource group name, e.g., vsclk-online-prod.
        /// </summary>
        string EnvironmentResourceGroupName { get; }

        /// <summary>
        /// Gets the control-plane enivronment key vault name, e.g., vsclk-online-prod-kv.
        /// </summary>
        string EnvironmentKeyVaultName { get; }

        /// <summary>
        /// Gets the control-plane instance resource group name, e.g., vsclk-online-prod-rel.
        /// </summary>
        string InstanceResourceGroupName { get; }

        /// <summary>
        /// Gets the control-plane instance cosmos db account name, e.g., vsclk-online-prod-rel-db.
        /// </summary>
        string InstanceCosmosDbAccountName { get; }

        /// <summary>
        /// Gets the control-plane instance maps account name, e.g., vsclk-online-prod-rel-maps.
        /// </summary>
        string InstanceMapsAccountName { get; }

        /// <summary>
        /// Gets the control-plane DNS host name.
        /// </summary>
        string DnsHostName { get; }

        /// <summary>
        /// Gets the VM agent container name.
        /// </summary>
        string VirtualMachineAgentContainerName { get; }

        /// <summary>
        /// Gets the File Share Template Container Name.
        /// </summary>
        string FileShareTemplateContainerName { get; }

        /// <summary>
        /// Gets the control-plane stamp info.
        /// </summary>
        IControlPlaneStampInfo Stamp { get; }

        /// <summary>
        /// Gets all control-plane stamp infos.
        /// </summary>
        IReadOnlyDictionary<AzureLocation, IControlPlaneStampInfo> AllStamps { get; }

        /// <summary>
        /// Gets the configured control-plane subscription id -- to support localhost development.
        /// Use <see cref="IControlPlaneAzureResourceAccessor.GetCurrentSubscriptionIdAsync"/> to get the live subscription id.
        /// </summary>
        /// <param name="subscriptionId">The subscription id result.</param>
        /// <returns>True if the subscription id is set in configuration, otherwise false.</returns>
        bool TryGetSubscriptionId(out string subscriptionId);

        /// <summary>
        /// Gets a list of all available data plane locations (global). E.g. places where you can provision a cloud environment.
        /// </summary>
        /// <returns>An enumerable containing the set of supported locations.</returns>
        IEnumerable<AzureLocation> GetAllDataPlaneLocations();

        /// <summary>
        /// Get the list of available control plane stamps.
        /// </summary>
        /// <returns>TBD</returns>
        IEnumerable<IControlPlaneStampInfo> GetControlPlaneStamps();

        /// <summary>
        /// Gets the owning control plane stamp for a given data plane location.
        /// </summary>
        /// <param name="dataPlaneLocation">The data plane location to search for.</param>
        /// <returns>The location of the owning control plane location.</returns>
        /// <exception cref="ArgumentException">Thrown if the data plane location is not supported.</exception>
        IControlPlaneStampInfo GetOwningControlPlaneStamp(AzureLocation dataPlaneLocation);
    }
}
