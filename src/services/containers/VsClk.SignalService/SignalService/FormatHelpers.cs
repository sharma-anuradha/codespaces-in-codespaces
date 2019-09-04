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
                case Properties.Email:
                    return "E";
                case Properties.ContactId:
                case Properties.IdReserved:
                    return "T";
                default:
                    return null;
            }
        }
    }
}

