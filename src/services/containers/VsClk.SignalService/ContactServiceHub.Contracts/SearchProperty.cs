// <copyright file="SearchProperty.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// The property search entity.
    /// </summary>
    public class SearchProperty
    {
        /// <summary>
        /// Gets or sets regular expression to apply into a property.
        /// </summary>
        public string Expression { get; set; }

        /// <summary>
        /// Gets or sets options to the regular expression.
        /// </summary>
        public int? Options { get; set; }
    }
}
