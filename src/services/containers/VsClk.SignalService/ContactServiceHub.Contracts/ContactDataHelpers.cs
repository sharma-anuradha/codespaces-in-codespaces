// <copyright file="ContactDataHelpers.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using ConnectionProperties = System.Collections.Generic.IDictionary<string, Microsoft.VsCloudKernel.SignalService.PropertyValue>;
using ConnectionsProperties = System.Collections.Generic.IDictionary<string, System.Collections.Generic.IDictionary<string, Microsoft.VsCloudKernel.SignalService.PropertyValue>>;
using ContactDataInfo = System.Collections.Generic.IDictionary<string, System.Collections.Generic.IDictionary<string, System.Collections.Generic.IDictionary<string, Microsoft.VsCloudKernel.SignalService.PropertyValue>>>;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Contact data helpers.
    /// </summary>
    public static class ContactDataHelpers
    {
        public static Dictionary<string, object> GetAggregatedProperties(this ContactDataInfo contactDataInfo)
        {
            return GetAggregatedProperties(contactDataInfo.Values.SelectMany(i => i.Values));
        }

        public static int GetConnectionsCount(this ContactDataInfo contactDataInfo)
        {
            return contactDataInfo.Values.Sum(item => item.Count);
        }

        public static Dictionary<string, ConnectionProperties> GetConnections(this ContactDataInfo contactDataInfo)
        {
            return contactDataInfo.Values.SelectMany(i => i).ToDictionary(p => p.Key, p => p.Value);
        }

        public static Dictionary<string, object> GetProperties(this ConnectionProperties connectionProperties)
        {
            return connectionProperties.ToDictionary(p => p.Key, p => p.Value.Value);
        }

        public static Dictionary<string, Dictionary<string, object>> GetProperties(this IDictionary<string, ConnectionProperties> connectionsProperties)
        {
            return connectionsProperties.ToDictionary(p => p.Key, p => p.Value.GetProperties());
        }

        public static Dictionary<string, object> GetAggregatedProperties(IEnumerable<ConnectionProperties> connectionsProperties)
        {
            return connectionsProperties.SelectMany(c => c)
                .GroupBy(p => p.Key)
                .ToDictionary(p => p.Key, p => GetLatestValue(p.Select(i => i.Value)));
        }

        public static object GetLatestValue(this IEnumerable<PropertyValue> propertyValues)
        {
            return propertyValues.OrderByDescending(pv => pv.Updated).FirstOrDefault().Value;
        }

        public static ConnectionProperties GetConnectionProperties(this ContactDataChanged<ContactDataInfo> contactDataChanged)
        {
            if (contactDataChanged.Data.TryGetValue(contactDataChanged.ServiceId, out var serviceConnections) &&
                serviceConnections.TryGetValue(contactDataChanged.ConnectionId, out var connectionProperties))
            {
                return connectionProperties;
            }

            return new Dictionary<string, PropertyValue>();
        }

        public static void UpdateConnectionProperties(this ContactDataInfo contactDataInfo, ContactDataChanged<ConnectionProperties> contactDataChanged)
        {
            ConnectionsProperties connectionsProperties;
            if (!contactDataInfo.TryGetValue(contactDataChanged.ServiceId, out connectionsProperties))
            {
                connectionsProperties = new Dictionary<string, IDictionary<string, PropertyValue>>();
                contactDataInfo[contactDataChanged.ServiceId] = connectionsProperties;
            }

            if (contactDataChanged.ChangeType == ContactUpdateType.Unregister)
            {
                connectionsProperties.Remove(contactDataChanged.ConnectionId);
            }
            else
            {
                if (connectionsProperties.TryGetValue(contactDataChanged.ConnectionId, out var connectionProperties))
                {
                    foreach (var kvp in contactDataChanged.Data)
                    {
                        connectionProperties[kvp.Key] = kvp.Value;
                    }
                }
                else
                {
                    connectionsProperties[contactDataChanged.ConnectionId] = contactDataChanged.Data.Clone();
                }
            }

            contactDataInfo.CleanupServiceConnections();
        }

        public static void CleanupServiceConnections(this ContactDataInfo contactDataInfo)
        {
            // Note: next block will eliminate the empty buckets for a service that has no connections
            foreach (var kvp in contactDataInfo.Where(kvp => kvp.Value.Count == 0).ToArray())
            {
                contactDataInfo.Remove(kvp.Key);
            }
        }

        public static ContactDataInfo Clone(this ContactDataInfo contactDataInfo)
        {
            return (ContactDataInfo)contactDataInfo.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (ConnectionsProperties)kvp.Value.ToDictionary(
                        kvp2 => kvp2.Key,
                        kvp2 => kvp2.Value.Clone()));
        }

        private static ConnectionProperties Clone(this ConnectionProperties connectionProperties)
        {
            return connectionProperties.ToDictionary(connectionValueKvp => connectionValueKvp.Key, connectionValueKvp => connectionValueKvp.Value);
        }
    }
}
