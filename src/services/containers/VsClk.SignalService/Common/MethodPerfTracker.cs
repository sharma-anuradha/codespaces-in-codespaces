// <copyright file="MethodPerfTracker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Diagnostics;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// A method perf tracker class.
    /// </summary>
    public class MethodPerfTracker : IDisposable
    {
        private readonly Action<TimeSpan> methodPerfTrackerCallback;
        private readonly Stopwatch start;

        public MethodPerfTracker(Action<TimeSpan> methodPerfTrackerCallback)
        {
            this.methodPerfTrackerCallback = Requires.NotNull(methodPerfTrackerCallback, nameof(methodPerfTrackerCallback));
            this.start = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            this.methodPerfTrackerCallback(this.start.Elapsed);
        }
    }
}
