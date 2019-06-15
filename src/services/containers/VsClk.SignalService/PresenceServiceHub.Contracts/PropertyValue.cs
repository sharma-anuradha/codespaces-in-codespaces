using System;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Represent a property value with both the value and the last time it was updated
    /// </summary>
    public struct PropertyValue
    {
        public PropertyValue(object value, DateTime updated)
        {
            Value = value;
            Updated = updated;
        }

        /// <summary>
        /// Value of the property
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// Last time it was updated
        /// </summary>
        public DateTime Updated { get; }
    }
}
