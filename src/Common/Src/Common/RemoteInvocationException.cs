// <copyright file="RemoteInvocationException.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// A remote invocation exception.
    /// </summary>
    public class RemoteInvocationException : RemoteException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteInvocationException"/> class.
        /// </summary>
        public RemoteInvocationException()
            : this("Exception thrown by RPC remote method invocation.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteInvocationException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public RemoteInvocationException(string message)
            : base(message, ErrorCodes.InvocationException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteInvocationException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="remoteStackTrace">The remote stack trace.</param>
        public RemoteInvocationException(string message, string remoteStackTrace = null)
            : this(message, ErrorCodes.InvocationException, remoteStackTrace)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteInvocationException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="remoteErrorCode">The remote error code.</param>
        /// <param name="remoteStackTrace">The remote stack trace.</param>
        public RemoteInvocationException(
            string message, int remoteErrorCode, string remoteStackTrace = null)
            : base(message, remoteErrorCode)
        {
            if (remoteStackTrace != null)
            {
                this.SetDataValue(nameof(StackTrace), remoteStackTrace);
            }
        }

        /// <summary>
        /// Gets the remote stack trace.
        /// </summary>
        public string RemoteStackTrace => this.GetDataValue<string>(nameof(StackTrace));
    }
}
