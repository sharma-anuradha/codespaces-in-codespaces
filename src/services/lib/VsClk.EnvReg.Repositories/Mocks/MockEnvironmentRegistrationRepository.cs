using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsCloudKernel.Services.EnvReg.Repositories
{
#if DEBUG

    public class MockEnvironmentRegistrationRepository : IEnvironmentRegistrationRepository
    {
        private IList<EnvironmentRegistration> _store = new List<EnvironmentRegistration>();

        public Task<EnvironmentRegistration> CreateAsync(EnvironmentRegistration document, IDiagnosticsLogger logger)
        {
            document.Id = Guid.NewGuid().ToString();
            this._store.Add(document);
            return Task.FromResult<EnvironmentRegistration>(document);
        }

        public async Task<EnvironmentRegistration> CreateOrUpdateAsync(EnvironmentRegistration document, IDiagnosticsLogger logger)
        {
            return await (string.IsNullOrEmpty(document.Id) ?
                CreateAsync(document, logger) : UpdateAsync(document, logger));
        }

        public async Task<bool> DeleteAsync(DocumentDbKey key, IDiagnosticsLogger logger)
        {
            var item = await GetAsync(key, logger);
            if (item != null)
            {
                this._store.Remove(item);
                return true;
            }
            return false;
        }

        public Task<EnvironmentRegistration> GetAsync(DocumentDbKey key, IDiagnosticsLogger logger)
        {
            return Task.FromResult<EnvironmentRegistration>(this._store.Where(x => x.Id == key.Id).FirstOrDefault());
        }

        public Task<IEnumerable<EnvironmentRegistration>> GetWhereAsync(Expression<Func<EnvironmentRegistration, bool>> where, IDiagnosticsLogger logger, Func<IEnumerable<EnvironmentRegistration>, IDiagnosticsLogger, Task> pageResultsCallback = null)
        {
            return Task.FromResult<IEnumerable<EnvironmentRegistration>>(this._store.Where(where.Compile()));
        }

        public async Task<EnvironmentRegistration> UpdateAsync(EnvironmentRegistration document, IDiagnosticsLogger logger)
        {
            await DeleteAsync(new DocumentDbKey(document.Id), logger);
            await CreateAsync(document, logger);
            return document;
        }
    }

#endif // DEBUG
}
