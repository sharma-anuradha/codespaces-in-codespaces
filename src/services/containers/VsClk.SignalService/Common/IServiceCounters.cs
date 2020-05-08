// <copyright file="IServiceCounters.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Define a contract to track perf service counters.
    /// The contract would except data to be tracked with the following format:
    /// "serviceA" ->
    ///                 "method1" -> (N times/Total time)
    ///                 "method2" -> (N times/Total time)
    ///                 etc..
    /// "serviceB" ->
    ///                 "method1" -> (N times/Total time)
    ///                 "method2" -> (N times/Total time)
    ///                 etc..
    /// </summary>
    public interface IServiceCounters
    {
        /// <summary>
        /// Return the current state of the perf counters being tracked.
        /// </summary>
        /// <returns>A dictionary map of a service name that map to another dictionary of method names and count/total time.</returns>
        Dictionary<string, Dictionary<string, (int, TimeSpan)>> GetPerfCounters();

        /// <summary>
        /// Reset the perf counters.
        /// </summary>
        void ResetCounters();

        /// <summary>
        /// Track an invocation of method that belong to service by adding a new entry on the service/method
        /// </summary>
        /// <param name="serviceName">Name of the service to track.</param>
        /// <param name="methodName">Name of the method to track.</param>
        /// <param name="timeSpan">Optional time take on this execution.</param>
        void OnInvokeMethod(string serviceName, string methodName, TimeSpan timeSpan);
    }
}
