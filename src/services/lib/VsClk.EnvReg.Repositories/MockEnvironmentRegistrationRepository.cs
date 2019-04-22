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
    public class MockEnvironmentRegistrationRepository : IEnvironmentRegistrationRepository
    {
        private IList<EnvironmentRegistration> _store = new List<EnvironmentRegistration>();

        public async Task<EnvironmentRegistration> CreateAsync(EnvironmentRegistration document, IDiagnosticsLogger logger)
        {
            document.Id = Guid.NewGuid().ToString();
            this._store.Add(document);
            return document;
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

        public async Task<EnvironmentRegistration> GetAsync(DocumentDbKey key, IDiagnosticsLogger logger)
        {
            return this._store.Where(x => x.Id == key.Id).FirstOrDefault();
        }

        public async Task<IEnumerable<EnvironmentRegistration>> GetWhereAsync(Expression<Func<EnvironmentRegistration, bool>> where, IDiagnosticsLogger logger, Func<IEnumerable<EnvironmentRegistration>, IDiagnosticsLogger, Task> pageResultsCallback = null)
        {
            return this._store.Where(where.Compile());
        }

        public async Task<EnvironmentRegistration> UpdateAsync(EnvironmentRegistration document, IDiagnosticsLogger logger)
        {
            await DeleteAsync(new DocumentDbKey(document.Id), logger);
            await CreateAsync(document, logger);
            return document;
        }
    }
}
