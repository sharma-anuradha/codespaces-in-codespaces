// <copyright file="ISecretProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// A provider of secrets. Shhhhh!.
    /// </summary>
    public interface ISecretProvider
    {
        /// <summary>
        /// Get a secret value by name.
        /// </summary>
        /// <param name="secretName">The secret name.</param>
        /// <returns>The secret value.</returns>
        /// <exception cref="InvalidOperationException">The secret could not be obtained.</exception>
        Task<string> GetSecretAsync(string secretName);
    }
}
