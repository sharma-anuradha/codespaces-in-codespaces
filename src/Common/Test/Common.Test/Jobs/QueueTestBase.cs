using System;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Test
{
    public abstract class QueueTestBase
    {
        protected static readonly IDiagnosticsLogger NullLogger = new Common.VsoUtil.NullLogger();

        protected QueueTestBase(IQueueFactory queueFactory)
        {
            QueueFactory = queueFactory;
        }

        protected IQueueFactory QueueFactory { get; }

        protected async Task RunQueueTest(Func<IQueue, Task> testCallback)
        {
            var queue = QueueFactory.GetOrCreate(Guid.NewGuid().ToString());
            try
            {
                await testCallback(queue);
            }
            finally
            {
                await DisposeIfAsync(queue);
           }
        }

        protected static async Task DisposeIfAsync(object o)
        {
            var disposable = o as IAsyncDisposable;
            if (disposable != null)
            {
                await disposable.DisposeAsync();
            }
        }
    }
}
