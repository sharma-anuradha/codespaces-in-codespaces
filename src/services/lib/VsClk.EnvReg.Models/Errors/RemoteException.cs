using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace VsClk.EnvReg.Models.Errors
{
    /// <summary>
    /// Relays details about an unhandled exception thrown by the remote handler of an RPC request.
    /// </summary>
    public class RemoteException : Exception
    {

        public RemoteException(string message, int remoteErrorCode)
            : base(message)
        {
            // Store extended properties in the standard exception data dictionary
            // so they are accessible even to an exception-handler that doesn't
            // know anything specific about the RemoteException class.
            Data[nameof(RemoteErrorCode)] = remoteErrorCode;
        }

        public int RemoteErrorCode => (int)Data[nameof(RemoteErrorCode)];
    }
}
