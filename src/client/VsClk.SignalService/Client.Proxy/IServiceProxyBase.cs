// <copyright file="IServiceProxyBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// The proxy service base contract.
    /// </summary>
    public interface IServiceProxyBase
    {
        /// <summary>
        /// Gets the underlying hub proxy.
        /// </summary>
        IHubProxy HubProxy { get; }
    }
}
