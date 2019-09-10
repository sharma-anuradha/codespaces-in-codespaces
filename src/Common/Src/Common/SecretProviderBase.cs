// <copyright file="SecretProviderBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Secrets that are passed in to the service as config at runtime.
    /// </summary>
    public abstract class SecretProviderBase : ISecretProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SecretProviderBase"/> class.
        /// </summary>
        public SecretProviderBase()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SecretProviderBase"/> class.
        /// </summary>
        /// <param name="values">The initial set of secret values.</param>
        public SecretProviderBase(IDictionary<string, string> values)
        {
            if (values != null)
            {
                foreach (var item in values)
                {
                    SetSecret(item.Key, item.Value);
                }
            }
        }

        private Dictionary<string, string> Values { get; } = new Dictionary<string, string>();

        /// <inheritdoc/>
        public async Task<string> GetSecretAsync(string secretName)
        {
            Requires.NotNullOrEmpty(secretName, nameof(secretName));
            await Task.CompletedTask;

            if (!TryGetSecret(secretName, out var secretValue))
            {
                throw new InvalidOperationException($"Secret not found: {secretName}");
            }

            return secretValue;
        }

        /// <summary>
        /// Sets a secret value.
        /// </summary>
        /// <param name="secretName">The secret name.</param>
        /// <param name="secretValue">The secret value.</param>
        protected void SetSecret(string secretName, string secretValue)
        {
            Requires.NotNullOrEmpty(secretName, nameof(secretName));
            Values[secretName] = secretValue;
        }

        /// <summary>
        /// Try to get a secret value by name.
        /// </summary>
        /// <param name="secretName">The secret name.</param>
        /// <param name="secretValue">The secret value.</param>
        /// <returns>True if the secret value is set.</returns>
        protected bool TryGetSecret(string secretName, out string secretValue)
        {
            return Values.TryGetValue(secretName, out secretValue);
        }
    }
}
