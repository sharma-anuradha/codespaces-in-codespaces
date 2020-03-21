// <copyright file="ContinuationInputExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation
{
    /// <summary>
    /// Cloninuation input extensions.
    /// </summary>
    public static class ContinuationInputExtensions
    {
        /// <summary>
        /// Clones continuation input with a specfici continuation token.
        /// </summary>
        /// <typeparam name="T">Type that you want the clone to be cast to.</typeparam>
        /// <param name="source">Source object to be cloned.</param>
        /// <param name="nextContinuationToken">Continuation token that should be used
        /// in the clone.</param>
        /// <returns>Cloned object.</returns>
        public static T BuildNextInput<T>(this T source, string nextContinuationToken)
            where T : ContinuationInput
        {
            // When there is no continuation, there is no next input
            if (string.IsNullOrEmpty(nextContinuationToken))
            {
                return null;
            }

            // Clone the current input and set the next continuation
            var newInput = source.Clone<T>();
            newInput.ContinuationToken = nextContinuationToken;

            return newInput;
        }
    }
}
