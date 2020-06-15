// <copyright file="MockClientAuthRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.LiveShareAuthentication;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LiveshareAuthentication
{
    /// <inheritdoc/>
    public class MockClientAuthRepository : IAuthRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MockClientAuthRepository"/> class.
        /// </summary>
        public MockClientAuthRepository()
        {
        }

        /// <inheritdoc/>
        public Task<string> ExchangeToken(string externalToken)
        {
            return Task.FromResult(externalToken);
        }
    }
}
