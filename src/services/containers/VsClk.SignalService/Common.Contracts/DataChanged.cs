// <copyright file="DataChanged.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Base class for our data changed structures.
    /// </summary>
    public class DataChanged
    {
        protected DataChanged(string changeId)
        {
            Requires.NotNullOrEmpty(changeId, nameof(changeId));
            ChangeId = changeId;
        }

        /// <summary>
        /// Gets unique change id.
        /// </summary>
        public string ChangeId { get; }
    }
}
