using System;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite
{
    public class RuntimeSecrets
    {
        public static string KeychainHashKey1 { get; set; }
        public static string KeychainHashId1 { get; set; }
        public static DateTime KeychainHashExpiration1 { get; set; }

        public static string KeychainHashKey2 { get; set; }
        public static string KeychainHashId2 { get; set; }
        public static DateTime KeychainHashExpiration2 { get; set; }

        private static TaskCompletionSource<bool> KeychainKeysInitalizedSignal = new TaskCompletionSource<bool>(false);
        private static Task<bool> KeychainKeysInitalized = KeychainKeysInitalizedSignal.Task;

        public static void ResolveKeychainSettingsSignal()
        {
            KeychainKeysInitalizedSignal
                .SetResult(true);
        }

        public static async Task WaitOnKeychainSettingsAsync(int delayMs = 10000)
        {
            var completed = await Task.WhenAny(KeychainKeysInitalizedSignal.Task, Task.Delay(delayMs));

            if (completed != KeychainKeysInitalizedSignal.Task)
            {
                throw new Exception("Failed to fetch client keychain settings.");
            }
        }
    }
}
