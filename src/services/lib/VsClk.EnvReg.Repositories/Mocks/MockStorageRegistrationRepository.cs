using Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore;

namespace Microsoft.VsCloudKernel.Services.EnvReg.Repositories
{
    public class MockStorageRegistrationRepository : MockRepository<FileShare>, IStorageRegistrationRepository
    {
    }
}
