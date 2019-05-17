using System;

namespace Microsoft.VsSaaS.Azure.Storage.FileShare
{
    public class FileShareProviderOptions
    {
        public string AccountName { get; set; }
        public string AccountKey { get; set; }

        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan RequestRetryBackoff { get; set; } = TimeSpan.FromSeconds(5);
        public int MaxRetryCount { get; set; } = 3;
    }
}