using System;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Hub Methods defined by presence service
    /// </summary>
    public static class Methods
    {
        private const string AsyncSuffix = "Async";

        public static string UpdateValues = ToHubName(nameof(IPresenceServiceClientHub.UpdateValuesAsync));
        public static string ReceiveMessage = ToHubName(nameof(IPresenceServiceClientHub.ReceiveMessageAsync));

        private static string ToHubName(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            string rpcName = ToCamelCase(name);

            if (rpcName.EndsWith(AsyncSuffix))
            {
                rpcName = rpcName.Substring(0, rpcName.Length - AsyncSuffix.Length);
            }

            return rpcName;
        }

        private static string ToCamelCase(string s)
        {
            return s.Length == 0 ? string.Empty :
                s.Substring(0, 1).ToLowerInvariant() + s.Substring(1);
        }
    }
}
