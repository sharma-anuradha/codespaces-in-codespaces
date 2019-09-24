// <copyright file="LinuxVirtualMachineManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Runtime.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine
{
    [Serializable]
    internal class VirtualMachineException : Exception
    {
        public VirtualMachineException()
        {
        }

        public VirtualMachineException(string message) : base(message)
        {
        }

        public VirtualMachineException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected VirtualMachineException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}