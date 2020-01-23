// <copyright file="ContinuationInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation
{
    /// <summary>
    /// Represents the continuation input.
    /// </summary>
    public class ContinuationInput
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContinuationInput"/> class.
        /// </summary>
        public ContinuationInput()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContinuationInput"/> class.
        /// </summary>
        /// <param name="continuationToken">Target continuation token.</param>
        public ContinuationInput(string continuationToken)
        {
            ContinuationToken = continuationToken;
        }

        /// <summary>
        /// Gets or sets the continuation token for operation.
        /// </summary>
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Clones current object.
        /// </summary>
        /// <typeparam name="T">Type that you want the clone to be cast to.</typeparam>
        /// <returns>Cloned object.</returns>
        public virtual T Clone<T>()
        {
            return (T)MemberwiseClone();
        }
    }
}
