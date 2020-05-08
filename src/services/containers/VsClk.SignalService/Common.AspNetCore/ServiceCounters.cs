// <copyright file="ServiceCounters.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Implements IServiceCounters interface.
    /// </summary>
    public class ServiceCounters : IServiceCounters
    {
        private readonly Dictionary<string, Dictionary<string, (int, TimeSpan)>> hubMethodCounters = new Dictionary<string, Dictionary<string, (int, TimeSpan)>>();
        private readonly object hubMethodCountersLock = new object();

        public Dictionary<string, Dictionary<string, (int, TimeSpan)>> GetPerfCounters()
        {
            lock (this.hubMethodCountersLock)
            {
                return this.hubMethodCounters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
            }
        }

        public void ResetCounters()
        {
            lock (this.hubMethodCountersLock)
            {
                this.hubMethodCounters.Clear();
            }
        }

        void IServiceCounters.OnInvokeMethod(string hubName, string hubMethod, TimeSpan timeSpan)
        {
            lock (this.hubMethodCountersLock)
            {
                Dictionary<string, (int, TimeSpan)> hubMethodCounters;
                if (!this.hubMethodCounters.TryGetValue(hubName, out hubMethodCounters))
                {
                    hubMethodCounters = new Dictionary<string, (int, TimeSpan)>();
                    this.hubMethodCounters.Add(hubName, hubMethodCounters);
                }

                (int, TimeSpan) methodCounters;
                if (hubMethodCounters.TryGetValue(hubMethod, out methodCounters))
                {
                    methodCounters = (methodCounters.Item1 + 1, methodCounters.Item2.Add(timeSpan));
                }
                else
                {
                    methodCounters = (1, timeSpan);
                }

                hubMethodCounters[hubMethod] = methodCounters;
            }
        }
    }
}
