// <copyright file="ContactConnectionProperties.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VsCloudKernel.SignalService.Common;
using ConnectionProperties = System.Collections.Generic.IDictionary<string, Microsoft.VsCloudKernel.SignalService.PropertyValue>;
using ConnectionsProperties = System.Collections.Generic.IDictionary<string, System.Collections.Generic.IDictionary<string, Microsoft.VsCloudKernel.SignalService.PropertyValue>>;

namespace Microsoft.VsCloudKernel.SignalService
{
    internal class ContactConnectionProperties
    {
        private readonly object selfConnectionPropertiesLock = new object();
        private readonly MessagePackDataBuffer<Dictionary<string, Dictionary<string, PropertyValue>>> selfConnectionPropertiesBuffer = new MessagePackDataBuffer<Dictionary<string, Dictionary<string, PropertyValue>>>(new Dictionary<string, Dictionary<string, PropertyValue>>());

        public ContactConnectionProperties()
        {
        }

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

        /// <summary>
        /// Gets all the connection properties for each of the registered end points.
        /// </summary>
        public ConnectionsProperties ConnectionsProperties
        {
            get
            {
                lock (this.selfConnectionPropertiesLock)
                {
                    return SelfConnectionProperties.ToDictionary(
                        connectionKvp => connectionKvp.Key,
                        connectionKvp => (ConnectionProperties)connectionKvp.Value.ToDictionary(propertyKvp => propertyKvp.Key, propertyKvp => propertyKvp.Value));
                }
            }
        }

        public KeyValuePair<string, ConnectionProperties>[] AllConnectionValues
        {
            get
            {
                lock (this.selfConnectionPropertiesLock)
                {
                    return SelfConnectionProperties.Select(kvp => new KeyValuePair<string, ConnectionProperties>(kvp.Key, kvp.Value.Clone())).ToArray();
                }
            }
        }

        public ConnectionProperties[] AllConnectionProperties
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
        /// Gets properties mantained by each of the live connections
        /// Key: connection Id
        /// Value: A dictionary property info structure with the value and the timestamp when it was updated.
        /// </summary>
        private Dictionary<string, Dictionary<string, PropertyValue>> SelfConnectionProperties => this.selfConnectionPropertiesBuffer.Data;

        public bool HasConnection(string connectionId)
        {
            lock (this.selfConnectionPropertiesLock)
            {
               return SelfConnectionProperties.ContainsKey(connectionId);
            }
        }

        public void MergeProperty(string connectionId, string propertyName, object value, DateTime updated)
        {
            lock (this.selfConnectionPropertiesLock)
            {
                var propertyValue = new PropertyValue(value, updated);
                this.selfConnectionPropertiesBuffer.GetAndSet(data =>
                {
                    data.AddOrUpdate(
                        connectionId,
                        (properties) => properties[propertyName] = propertyValue);
                });
            }
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
                this.selfConnectionPropertiesBuffer.GetAndSet(data => data[connectionId] = new Dictionary<string, PropertyValue>());
            }
        }

        public string[] RemoveConnectionProperties(string connectionId)
        {
            lock (this.selfConnectionPropertiesLock)
            {
                string[] propertyKeys = null;

                this.selfConnectionPropertiesBuffer.GetAndSet(data =>
                {
                    if (data.Remove(connectionId, out var properties))
                    {
                        propertyKeys = properties.Keys.ToArray();
                    }
                });

                return propertyKeys ?? Array.Empty<string>();
            }
        }
    }
}
