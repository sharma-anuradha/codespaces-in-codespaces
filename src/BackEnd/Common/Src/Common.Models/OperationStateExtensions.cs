// <copyright file="OperationState.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models
{
    /// <summary>
    /// 
    /// </summary>
    public static class OperationStateExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="operationState"></param>
        /// <returns></returns>
        public static bool IsFinial(this OperationState operationState)
        {
            return operationState == OperationState.Succeeded
                || operationState == OperationState.Cancelled
                || operationState == OperationState.Failed;
        }
    }
}
