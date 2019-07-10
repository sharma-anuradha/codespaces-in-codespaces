using Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore;
using Microsoft.VsCloudKernel.Services.EnvReg.Repositories;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.Services.EnvReg.Repositories
{
#if DEBUG

    public class MockStorageRegistrationRepository : IStorageRegistrationRepository
    {
        public Task<FileShare> CreateAsync(FileShare document, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        public Task<FileShare> CreateOrUpdateAsync(FileShare document, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteAsync(DocumentDbKey key, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        public Task<FileShare> GetAsync(DocumentDbKey key, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<FileShare>> GetWhereAsync(Expression<Func<FileShare, bool>> where, IDiagnosticsLogger logger, Func<IEnumerable<FileShare>, IDiagnosticsLogger, Task> pageResultsCallback = null)
        {
            throw new NotImplementedException();
        }

        public Task<FileShare> UpdateAsync(FileShare document, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }
    }

#endif //DEBUG
}
