// <copyright file="FormatHelpers.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Format helpers for all our signalR hub services.
    /// </summary>
    internal static class FormatHelpers
    {
        public static string GetPropertyFormat(string propertyName)
        {
            switch (propertyName)
            {
                case ContactProperties.Email:
                    return "E";
                case ContactProperties.ContactId:
                case ContactProperties.IdReserved:
                    return "T";
                default:
                    return null;
            }
        }
    }
}
