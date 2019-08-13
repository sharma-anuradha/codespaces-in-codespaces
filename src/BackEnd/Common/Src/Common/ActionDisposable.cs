// <copyright file="DistributedLease.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Azure.Storage.Blob;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.WindowsAzure.Storage;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// 
    /// </summary>
    public class ActionDisposable : IDisposable
    {
        private ActionDisposable(Action action)
        {
            Action = action;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public static IDisposable Create(Action action)
        {
            return new ActionDisposable(action);
        }

        private Action Action { get; }

        public void Dispose()
        {
            Action();
        }
    }
}
