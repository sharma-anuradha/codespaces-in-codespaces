using System;
using System.Collections.Generic;
using System.Text;

namespace VsClk.EnvReg.Models.Errors
{
    public class RemoteInvocationException : RemoteException
    {
        public RemoteInvocationException()
            : this("Exception thrown by RPC remote method invocation.")
        {
        }

        public RemoteInvocationException(string message)
            : base(message, ErrorCodes.InvocationException)
        {
        }

        public RemoteInvocationException(string message, string remoteStackTrace = null)
            : this(message, ErrorCodes.InvocationException, remoteStackTrace)
        {
        }

        public RemoteInvocationException(
            string message, int remoteErrorCode, string remoteStackTrace = null)
            : base(message, remoteErrorCode)
        {
            if (remoteStackTrace != null)
            {
                Data[nameof(StackTrace)] = remoteStackTrace;
            }
        }

        public string RemoteStackTrace => (string)Data[nameof(StackTrace)];
    }
}
