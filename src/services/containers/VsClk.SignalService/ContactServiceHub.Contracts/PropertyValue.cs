// <copyright file="PropertyValue.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Represent a property value with both the value and the last time it was updated.
    /// </summary>
    public struct PropertyValue
    {
        public PropertyValue(object value, DateTime updated)
        {
            Value = value;
            Updated = updated;
        }

        /// <summary>
        /// Gets the value of the property.
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// gets the last time it was updated.
        /// </summary>
        public DateTime Updated { get; }
    }
}
