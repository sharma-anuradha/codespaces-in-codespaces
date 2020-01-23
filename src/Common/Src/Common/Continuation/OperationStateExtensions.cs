// <copyright file="OperationStateExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation
{
    /// <summary>
    /// Operation State Extensions.
    /// </summary>
    public static class OperationStateExtensions
    {
        /// <summary>
        /// Checks to see if the status is final.
        /// </summary>
        /// <param name="operationState">Target operational state.</param>
        /// <returns>Returns if it is final or not.</returns>
        public static bool IsFinal(this OperationState operationState)
        {
            return operationState == OperationState.Succeeded
                || operationState == OperationState.Cancelled
                || operationState == OperationState.Failed;
        }

        /// <summary>
        /// Checks to see if the status is final.
        /// </summary>
        /// <param name="operationState">Target operational state.</param>
        /// <returns>Returns if it is final or not.</returns>
        public static bool IsFinal(this OperationState? operationState)
        {
            return operationState.HasValue ? operationState.Value.IsFinal() : true;
        }
    }
}
