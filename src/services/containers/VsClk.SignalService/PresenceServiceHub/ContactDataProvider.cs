using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VsCloudKernel.SignalService
{
    using ContactDataInfo = IDictionary<string, IDictionary<string, IDictionary<string, PropertyValue>>>;

    /// <summary>
    /// Class to provide properties values from a source
    /// </summary>
    internal abstract class ContactDataProvider
    {
        public abstract Dictionary<string, object> Properties { get; }
        public abstract object GetConnectionPropertyValue(string propertyName, string connectionId);

        public static ContactDataProvider CreateContactDataProvider(ContactDataInfo contactDataInfo)
        {
            return new ContactDataInfoProvider(contactDataInfo);
        }

        public static ContactDataProvider CreateContactDataProvider(Dictionary<string, object> properties)
        {
            return new ContactPropertiesProvider(properties);
        }

        public static ContactDataProvider CreateContactDataProvider(Func<Dictionary<string, object>> valueFactory)
        {
            return new ContactPropertiesProvider(valueFactory);
        }

        /// <summary>
        /// Implementation based on a property bag
        /// </summary>
        private class ContactPropertiesProvider : ContactDataProvider
        {
            private Lazy<Dictionary<string, object>> properties;

            internal ContactPropertiesProvider(Func<Dictionary<string, object>> valueFactory)
            {
                this.properties = new Lazy<Dictionary<string, object>>(valueFactory);
            }

            internal ContactPropertiesProvider(Dictionary<string, object> properties)
                : this(() => properties)
            {
            }

            public override Dictionary<string, object> Properties => this.properties.Value;

            public override object GetConnectionPropertyValue(string propertyName, string connectionId) => null;
        }

        /// <summary>
        /// Implementation based on Contact data info type
        /// </summary>
        private class ContactDataInfoProvider : ContactDataProvider
        {
            private readonly ContactDataInfo contactDataInfo;
            private readonly Lazy<Dictionary<string, object>> properties;

            internal ContactDataInfoProvider(ContactDataInfo contactDataInfo)
            {
                this.contactDataInfo = contactDataInfo;
                this.properties = new Lazy<Dictionary<string, object>>(() => contactDataInfo.GetAggregatedProperties());
            }

            public override Dictionary<string, object> Properties => this.properties.Value;

            public override object GetConnectionPropertyValue(string propertyName, string connectionId)
            {
                object value = null;
                var connectionProperties = contactDataInfo.Values.FirstOrDefault(i => i.ContainsKey(connectionId));

                if (connectionProperties != null && connectionProperties.TryGetValue(connectionId, out var properties) &&
                    properties.TryGetValue(propertyName, out var pv))
                {
                    value = pv.Value;
                }

                return value;
            }
        }
    }
}
