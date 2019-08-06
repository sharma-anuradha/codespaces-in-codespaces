using Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;

namespace Microsoft.VsCloudKernel.Services.EnvReg.Repositories
{
    public interface IBillingEventRepository : IDocumentDbCollection<BillingEvent>
    {
    }
}
