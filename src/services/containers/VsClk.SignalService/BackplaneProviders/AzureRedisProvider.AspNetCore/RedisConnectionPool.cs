// <copyright file="RedisConnectionPool.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Linq;
using StackExchange.Redis;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Redis connection pool class
    /// </summary>
    public class RedisConnectionPool
    {
        public RedisConnectionPool(ConnectionMultiplexer[] connections)
        {
            Requires.NotNull(connections, nameof(connections));
            Requires.Argument(connections.Length > 0, nameof(connections), "Length == 0");
            SubscribeConnection = connections[0];
            Connections = connections.Length == 1 ? connections : connections.Skip(1).ToArray();
        }

        public IDatabaseAsync DatabaseAsync => GetNextConnection().GetDatabase();

        public ConnectionMultiplexer SubscribeConnection { get; }

        public ConnectionMultiplexer[] Connections { get; }

        public ConnectionMultiplexer GetNextConnection()
        {
            return Connections.OrderBy(c => c.GetCounters().TotalOutstanding).First();
        }
    }
}
