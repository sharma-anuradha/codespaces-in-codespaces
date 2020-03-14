// <copyright file="IRelayDataHubProxy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// A relay data hub proxy.
    /// </summary>
    public interface IRelayDataHubProxy
    {
        /// <summary>
        /// When data is recieved.
        /// </summary>
        event EventHandler<ReceiveDataEventArgs> ReceiveData;
    }
}
