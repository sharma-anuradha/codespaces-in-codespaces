// <copyright file="IDataFormatProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Service format provider.
    /// </summary>
    public interface IDataFormatProvider : IFormatProvider
    {
        /// <summary>
        /// Return the proper format string for a specific property name.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The format to use or null if none.</returns>
        string GetPropertyFormat(string propertyName);
    }
}
