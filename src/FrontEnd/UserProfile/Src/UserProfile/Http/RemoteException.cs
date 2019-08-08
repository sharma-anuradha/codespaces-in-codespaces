// <copyright file="RemoteException.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile.Http
{
    /// <summary>
    /// Relays details about an unhandled exception thrown by the remote handler of an RPC request.
    /// </summary>
    public class RemoteException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="remoteErrorCode">The remote error code.</param>
        public RemoteException(string message, int remoteErrorCode)
            : base(message)
        {
            // Store extended properties in the standard exception data dictionary
            // so they are accessible even to an exception-handler that doesn't
            // know anything specific about the RemoteException class.
            this.SetDataValue(nameof(RemoteErrorCode), remoteErrorCode);
        }

        /// <summary>
        /// Gets the remote error code.
        /// </summary>
        public int RemoteErrorCode => this.GetDataValue<int>(nameof(RemoteErrorCode));
    }
}
