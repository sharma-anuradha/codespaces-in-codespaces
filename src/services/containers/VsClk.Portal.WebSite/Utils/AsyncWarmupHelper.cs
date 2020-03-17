using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common.Warmup;

namespace Microsoft.VsCloudKernel.Services.Portal.WebSite.Utils
{
    public class AsyncWarmupHelper
    {
        private readonly IEnumerable<IAsyncWarmup> asyncWarmups;

        public AsyncWarmupHelper(IEnumerable<IAsyncWarmup> asyncWarmups)
        {
            this.asyncWarmups = asyncWarmups;
        }

        public async Task RunAsync()
        {
            await Task.WhenAll(this.asyncWarmups.Select((warmup) => warmup.WarmupCompletedAsync()));
        }
    }
}
