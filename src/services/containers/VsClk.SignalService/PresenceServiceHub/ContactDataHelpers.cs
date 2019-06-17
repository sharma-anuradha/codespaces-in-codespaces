using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VsCloudKernel.SignalService
{
    using ConnectionProperties = IDictionary<string, PropertyValue>;
    using ConnectionsProperties = IDictionary<string, IDictionary<string, PropertyValue>>;
    using ContactDataInfo = IDictionary<string, IDictionary<string, IDictionary<string, PropertyValue>>>;

    /// <summary>
    /// Contact data helpers 
    /// </summary>
    public static class ContactDataHelpers
    {
        public static Dictionary<string, object> GetAggregatedProperties(this ContactDataInfo contactDataInfo)
        {
            return GetAggregatedProperties(contactDataInfo.Values.SelectMany(i => i.Values));
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

        public static void UpdateConnectionProperties(this ContactDataInfo contactDataInfo, ContactDataChanged<ConnectionProperties> contactDataChanged)
        {
            ConnectionsProperties connectionsProperties;
            if (!contactDataInfo.TryGetValue(contactDataChanged.ServiceId, out connectionsProperties))
            {
                connectionsProperties = new Dictionary<string, IDictionary<string, PropertyValue>>();
                contactDataInfo[contactDataChanged.ServiceId] = connectionsProperties;
            }

            if (contactDataChanged.Type == ContactUpdateType.Unregister)
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
                    connectionsProperties[contactDataChanged.ConnectionId] = contactDataChanged.Data;
                }
            }

            // Note: next block will eliminate the empty buckets for a service that no connections
            foreach(var kvp in contactDataInfo.Where(kvp => kvp.Value.Count == 0).ToArray())
            {
                contactDataInfo.Remove(kvp.Key);
            }
        }
    }
}
