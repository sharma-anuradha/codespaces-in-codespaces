using System;
using System.Diagnostics;
using Microsoft.AspNetCore.SignalR.Client;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    public class HubClientProxy<T> : HubClient
        where T : class
    {
        public HubClientProxy(string url, TraceSource trace)
            : this(FromUrl(url).Build(), trace)
        {
        }

        public HubClientProxy(string url, string accessToken, TraceSource trace)
            : this(FromUrlAndAccessToken(url, accessToken).Build(), trace)
        {
        }

        public HubClientProxy(string url, Func<string> accessTokenCallback, TraceSource trace)
            : this(FromUrlAndAccessToken(url, accessTokenCallback).Build(), trace)
        {
        }

        public HubClientProxy(HubConnection hubConnection, TraceSource trace)
            : base(hubConnection, trace)
        {
            Proxy = (T)Activator.CreateInstance(typeof(T), hubConnection, trace);
        }

        public T Proxy { get; }
    }
}
