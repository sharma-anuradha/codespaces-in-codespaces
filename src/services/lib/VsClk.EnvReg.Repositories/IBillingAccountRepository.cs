using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.Services.EnvReg.Repositories
{
    public interface IBillingAccountRepository : IDocumentDbCollection<BillingAccount>
    {

    }
}
