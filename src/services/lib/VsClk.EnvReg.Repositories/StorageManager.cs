using Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore;
using Microsoft.VsSaaS.Azure.Storage.FileShare;
using Microsoft.VsSaaS.Diagnostics;
using System;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.Services.EnvReg.Repositories
{
    public class StorageManager  : IStorageManager 
    {
        private readonly IFileShareProvider fileShareProvider;
        private readonly IStorageRegistrationRepository fileShareRepository;
        private readonly IDiagnosticsLoggerFactory loggerFactory;
        private readonly LogValueSet defaultLogValues;

        public StorageManager (
            [ValidatedNotNull] IFileShareProvider fileShareProvider,
            [ValidatedNotNull] IStorageRegistrationRepository fileShareRepository,
            [ValidatedNotNull] IDiagnosticsLoggerFactory loggerFactory,
            LogValueSet defaultLogValues)
        {
            Requires.NotNull(fileShareProvider, nameof(fileShareProvider));
            Requires.NotNull(fileShareRepository, nameof(fileShareRepository));
            Requires.NotNull(loggerFactory, nameof(loggerFactory));

            this.fileShareProvider = fileShareProvider;
            this.fileShareRepository = fileShareRepository;
            this.loggerFactory = loggerFactory;
            this.defaultLogValues = defaultLogValues;
        }

        public async Task<FileShare> CreateFileShareForEnvironmentAsync(FileShareEnvironmentInfo environmentInfo, IDiagnosticsLogger logger = null)
        {
            logger = GetLogger(logger);

            var shareName = CreateUniqueFileShareName();
            bool didCreateFileShare = await fileShareProvider.TryCreateFileShareWithNameAsync(shareName, logger);
            if (!didCreateFileShare)
            {
                return null;
            }

            var shareDocument = new FileShare {
                Id = Guid.NewGuid().ToString(),
                Name = shareName,
                EnvironmentInfo = environmentInfo
            };
            try
            {
                shareDocument = await fileShareRepository.CreateAsync(shareDocument, logger);

                return shareDocument;
            }
            catch (Exception)
            {
                await fileShareProvider.DeleteFileShareWithNameAsync(shareName, logger);
                throw;
            }
        }

        private IDiagnosticsLogger GetLogger(IDiagnosticsLogger logger)
        {
            if (logger != null)
            {
                return logger;
            }
            return loggerFactory.New(defaultLogValues);
        }

        private string CreateUniqueFileShareName()
        {
            return System.Guid.NewGuid().ToString();
        }
    }
}
