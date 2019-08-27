// <copyright file="ContinuationTaskMessageHandlerInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation
{
    /// <summary>
    /// 
    /// </summary>
    public class ContinuationTaskMessageHandlerInput
    {
        /// <summary>
        /// 
        /// </summary>
        public object Input { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public object Metadata { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OperationState? Status { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string ContinuationToken { get; set; }
    }
}
