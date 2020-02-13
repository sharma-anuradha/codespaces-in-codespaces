// <copyright file="IAzureRedisProviderServiceFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Factory interface to create Azure Redis providers
    /// </summary>
    public interface IAzureRedisProviderServiceFactory
    {
        Task CreateAsync(
            (string ServiceId, string Stamp) serviceInfo,
            RedisConnectionPool redisConnectionPool,
            CancellationToken cancellationToken);
    }
}
