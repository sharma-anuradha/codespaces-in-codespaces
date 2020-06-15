// <copyright file="IServiceUriBuilder.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Service Uri builder interface.
    /// </summary>
    public interface IServiceUriBuilder
    {
        /// <summary>
        /// Creates the service uri based on the request uri and the control stamp.
        /// </summary>
        /// <param name="requestUri">Request uri.</param>
        /// <param name="controlPlaneStampInfo">Control stamp info.</param>
        /// <returns>Uri of the service.</returns>
        Uri GetServiceUri(string requestUri, IControlPlaneStampInfo controlPlaneStampInfo);

        /// <summary>
        /// Creates the callback uri based on the request uri and the control stamp.
        /// </summary>
        /// <param name="requestUri">Request uri.</param>
        /// <param name="controlPlaneStampInfo">Control stamp info.</param>
        /// <returns>Uri of the callback.</returns>
        Uri GetCallbackUriFormat(string requestUri, IControlPlaneStampInfo controlPlaneStampInfo);
    }
}
