// <copyright file="QueueMessage.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine
{
    internal class QueueMessage
    {
        public string Command { get; set; }
        public IDictionary<string, string> Parameters { get; set; }
    }
}