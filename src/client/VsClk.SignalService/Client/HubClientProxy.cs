// <copyright file="HubClientProxy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.SignalR.Client;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// A hub client proxy that holds a hub client.
    /// </summary>
    /// <typeparam name="T">Type of proxy.</typeparam>
    public class HubClientProxy<T> : HubClient
        where T : class
    {
        public HubClientProxy(string url, TraceSource trace, bool useSignalRHub = false)
            : this(HubConnectionHelpers.FromUrl(url).Build(), trace, useSignalRHub)
        {
        }

        public HubClientProxy(string url, string accessToken, TraceSource trace, bool useSignalRHub = false)
            : this(HubConnectionHelpers.FromUrlAndAccessToken(url, accessToken).Build(), trace, useSignalRHub)
        {
        }

        public HubClientProxy(string url, Func<string> accessTokenCallback, TraceSource trace, bool useSignalRHub = false)
            : this(HubConnectionHelpers.FromUrlAndAccessToken(url, accessTokenCallback).Build(), trace, useSignalRHub)
        {
        }

        public HubClientProxy(HubConnection hubConnection, TraceSource trace, bool useSignalRHub = false)
            : base(hubConnection, trace)
        {
            Proxy = HubProxy.CreateHubProxy<T>(this, trace, useSignalRHub);
        }

        /// <summary>
        /// Gets the proxy instance of this hub connection.
        /// </summary>
        public T Proxy { get; }
    }
}
