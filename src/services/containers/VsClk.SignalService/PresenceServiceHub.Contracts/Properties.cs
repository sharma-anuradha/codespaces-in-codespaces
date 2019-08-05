// <copyright file="Properties.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Hub properties defined by presence service
    /// </summary>
    public static class Properties
    {
        /// <summary>
        /// The reserved '_Id' property
        /// </summary>
        public const string IdReserved = "_Id";

        /// <summary>
        /// Email primary property
        /// </summary>
        public const string Email = "email";

        /// <summary>
        /// The contact id property
        /// </summary>
        public const string ContactId = "contactId";

        /// <summary>
        /// The connection Id property
        /// </summary>
        public const string ConnectionId = "connectiondId";

        /// <summary>
        /// The service id property
        /// </summary>
        public const string ServiceId = "serviceId";
    }
}
