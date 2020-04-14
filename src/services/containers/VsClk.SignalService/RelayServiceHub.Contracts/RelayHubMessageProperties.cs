// <copyright file="RelayHubMessageProperties.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Factory relay hub message properties.
    /// </summary>
    public static class RelayHubMessageProperties
    {
        /// <summary>
        /// The message property unique id.
        /// </summary>
        public const string PropertySequenceId = "sequenceId";

        /// <summary>
        /// The audit property prefix id.
        /// </summary>
        public const string PropertyAuditPrefixId = "audit-";

        /// <summary>
        /// The target property  prefix id.
        /// </summary>
        public const string PropertyTargetPrefixId = "target-";

        /// <summary>
        /// Indicate the special 'data' property.
        /// </summary>
        public const string PropertyDataId = "data";

        /// <summary>
        /// Create message properties based on a sequence id.
        /// </summary>
        /// <param name="sequenceId">The next sequence id.</param>
        /// <returns>A message properties dictionary.</returns>
        public static Dictionary<string, object> CreateMessageSequence(int sequenceId)
        {
            return new Dictionary<string, object>()
            {
                { PropertySequenceId, sequenceId },
            };
        }
    }
}
