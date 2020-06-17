// <copyright file="LogHub.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace DiagnosticsServer.Hubs
{
    /// <summary>
    /// SignalR Hub for logs.
    /// </summary>
    public class LogHub : Hub
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogHub"/> class.
        /// </summary>
        public LogHub()
        {
        }
    }
}
