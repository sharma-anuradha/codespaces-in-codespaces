// <copyright file="ContactReference.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    ///  A contact reference entity.
    /// </summary>
    public struct ContactReference
    {
        public ContactReference(string id, string connectionId)
        {
            Requires.NotNullOrEmpty(id, nameof(id));
            Id = id;
            ConnectionId = connectionId;
        }

        /// <summary>
        /// Gets or sets the contact id to refer.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the optional connection id on this contact.
        /// </summary>
        public string ConnectionId { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return ToString(null);
        }

        public string ToString(IFormatProvider provider)
        {
            const string format = "{{ Id:{0:T} connectionId:{1} }}";

            return string.Format(provider, format, Id, ConnectionId);
        }
    }
}
