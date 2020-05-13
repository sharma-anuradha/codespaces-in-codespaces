// <copyright file="ContactConnectionProperties.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VsCloudKernel.SignalService.Common;

namespace Microsoft.VsCloudKernel.SignalService
{
    internal class ContactConnectionProperties
    {
        private readonly object selfConnectionPropertiesLock = new object();

#if DEBUG
        public ContactConnectionProperties()
        {
        }
#endif

        public string[] AllConnections
        {
            get
            {
                lock (this.selfConnectionPropertiesLock)
                {
                    return SelfConnectionProperties.Keys.ToArray();
                }
            }
        }

        public KeyValuePair<string, IDictionary<string, PropertyValue>>[] AllConnectionValues
        {
            get
            {
                lock (this.selfConnectionPropertiesLock)
                {
                    return SelfConnectionProperties.Select(kvp => new KeyValuePair<string, IDictionary<string, PropertyValue>>(kvp.Key, kvp.Value.Clone())).ToArray();
                }
            }
        }

        public IDictionary<string, PropertyValue>[] AllConnectionProperties
        {
            get
            {
                lock (this.selfConnectionPropertiesLock)
                {
                    return SelfConnectionProperties.Select(kvp => kvp.Value.Clone()).ToArray();
                }
            }
        }

        public int Count
        {
            get
            {
                lock (this.selfConnectionPropertiesLock)
                {
                    return SelfConnectionProperties.Count;
                }
            }
        }

        /// <summary>
        /// Gets properties maintained by each of the live connections
        /// Key: connection Id
        /// Value: A dictionary property info structure with the value and the timestamp when it was updated.
        /// </summary>
        private Dictionary<string, Dictionary<string, PropertyValue>> SelfConnectionProperties { get; } = new Dictionary<string, Dictionary<string, PropertyValue>>();

        public bool HasConnection(string connectionId)
        {
            lock (this.selfConnectionPropertiesLock)
            {
               return SelfConnectionProperties.ContainsKey(connectionId);
            }
        }

        public void MergeProperty(string connectionId, string propertyName, object value, DateTime updated)
        {
            var propertyValue = new PropertyValue(value, updated);
            SelfConnectionProperties.AddOrUpdate(
                connectionId,
                (properties) => properties[propertyName] = propertyValue,
                this.selfConnectionPropertiesLock);
        }

        public bool TryGetProperties(string connectionId, out Dictionary<string, PropertyValue> properties)
        {
            lock (this.selfConnectionPropertiesLock)
            {
                if (SelfConnectionProperties.TryGetValue(connectionId, out var value))
                {
                    properties = value.Clone();
                    return true;
                }

                properties = null;
                return false;
            }
        }

        public void AddConnection(string connectionId)
        {
            lock (this.selfConnectionPropertiesLock)
            {
                SelfConnectionProperties[connectionId] = new Dictionary<string, PropertyValue>();
            }
        }

        public string[] RemoveConnectionProperties(string connectionId)
        {
            lock (this.selfConnectionPropertiesLock)
            {
                if (SelfConnectionProperties.Remove(connectionId, out var properties))
                {
                    return properties.Keys.ToArray();
                }

                return Array.Empty<string>();
            }
        }
    }
}
