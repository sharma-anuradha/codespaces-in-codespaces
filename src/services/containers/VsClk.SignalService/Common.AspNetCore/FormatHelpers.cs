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
                // ContactProperties.Email
                case "email":
                    return "E";

                // ContactProperties.ContactId
                case "contactId":
                // ContactProperties.IdReserved
                case "_Id":
                    return "T";

                default:
                    return null;
            }
        }
    }
}
