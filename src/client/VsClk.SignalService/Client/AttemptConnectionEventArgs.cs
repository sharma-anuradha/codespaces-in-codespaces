// <copyright file="AttemptConnectionEventArgs.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// Event to be raised when a connection is attempted.
    /// </summary>
    public class AttemptConnectionEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AttemptConnectionEventArgs"/> class.
        /// </summary>
        /// <param name="retries">Number of retries.</param>
        /// <param name="backoffTimeMillisecs">Back off millisces.</param>
        /// <param name="error">Error being reported.</param>
        internal AttemptConnectionEventArgs(int retries, int backoffTimeMillisecs, Exception error)
        {
            Retries = retries;
            BackoffTimeMillisecs = backoffTimeMillisecs;
            Error = error;
        }

        /// <summary>
        /// Gets number of retries.
        /// </summary>
        public int Retries { get; }

        /// <summary>
        /// Gets or sets backoff time in millisecs.
        /// </summary>
        public int BackoffTimeMillisecs { get; set; }

        /// <summary>
        /// Gets error being reported.
        /// </summary>
        public Exception Error { get; }
    }
}
