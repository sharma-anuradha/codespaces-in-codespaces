// <copyright file="ContinuationTaskMessageHandlerResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation
{
    /// <summary>
    /// 
    /// </summary>
    public class ContinuationTaskMessageHandlerResult
    {
        /// <summary>
        /// 
        /// </summary>
        public ContinuationResult Result { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public object Metadata { get; set; }
    }
}
