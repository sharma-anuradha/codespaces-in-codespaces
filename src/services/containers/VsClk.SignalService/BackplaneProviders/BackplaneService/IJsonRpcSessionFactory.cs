// <copyright file="IJsonRpcSessionFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.IO;
using StreamJsonRpc;

namespace Microsoft.VsCloudKernel.BackplaneService
{
    /// <summary>
    /// Contract to define an rpc session factory.
    /// </summary>
    public interface IJsonRpcSessionFactory
    {
        string ServiceType { get; }

        void StartRpcSession(JsonRpc jsonRpc, string serviceId);
    }
}
