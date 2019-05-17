using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.File;
using Microsoft.WindowsAzure.Storage.RetryPolicies;

namespace Microsoft.VsSaaS.Azure.Storage.FileShare
{
    public class FileShareProvider : IFileShareProvider
    {
        private readonly FileShareProviderOptions options;
        private readonly CloudFileClient fileShareClient;
        private readonly IDiagnosticsLoggerFactory loggerFactory;
        private readonly LogValueSet defaultLogValues;

        public FileShareProvider(
            [ValidatedNotNull] IOptions<FileShareProviderOptions> options,
            [ValidatedNotNull] IDiagnosticsLoggerFactory loggerFactory,
            LogValueSet defaultLogValues)
        {
            Requires.NotNull(options, nameof(options));
            this.options = options.Value;

            var credentials = new StorageCredentials(this.options.AccountName, this.options.AccountKey);
            var storageAccount = new CloudStorageAccount(credentials, useHttps: true);

            fileShareClient = storageAccount.CreateCloudFileClient();
            this.loggerFactory = loggerFactory;
            this.defaultLogValues = defaultLogValues;
        }

        public async Task DeleteFileShareWithNameAsync([ValidatedNotNull] string name, IDiagnosticsLogger logger = null)
        {
            Requires.NotNullOrWhiteSpace(name, nameof(name));
            if (logger == null)
            {
                logger = this.loggerFactory.New(defaultLogValues);
            }

            var duration = logger.StartDuration();

            var shareReference = fileShareClient.GetShareReference(name);

            await shareReference.DeleteAsync(
                null,
                new FileRequestOptions
                {
                    ServerTimeout = this.options.RequestTimeout,
                    RetryPolicy = new LinearRetry(this.options.RequestRetryBackoff, this.options.MaxRetryCount)
                },
                null
            ).ConfigureAwait(false);

            logger.AddValue("fileShareName", name);
            logger.AddDuration(duration);
            logger.LogInfo("[FileShareProvider.DelteFileShareWithNameAsync] Finished deleting file share.");
        }

        public async Task<bool> TryCreateFileShareWithNameAsync([ValidatedNotNull] string name, IDiagnosticsLogger logger = null)
        {
            Requires.NotNullOrWhiteSpace(name, nameof(name));
            if (logger == null)
            {
                logger = this.loggerFactory.New(defaultLogValues);
            }

            var duration = logger.StartDuration();

            var shareReference = fileShareClient.GetShareReference(name);
            var didCreateFileShare = await shareReference.CreateIfNotExistsAsync(
                new FileRequestOptions
                {
                    ServerTimeout = this.options.RequestTimeout,
                    RetryPolicy = new LinearRetry(this.options.RequestRetryBackoff, this.options.MaxRetryCount)
                },
                null
            ).ConfigureAwait(false);

            logger.AddValue("fileShareName", name);
            logger.AddValue("success", didCreateFileShare.ToString().ToLower());
            logger.AddDuration(duration);

            logger.LogInfo("[FileShareProvider.CreateFileShareWithIdAsync] Finished creating file share.");

            return didCreateFileShare;
        }
    }
}
