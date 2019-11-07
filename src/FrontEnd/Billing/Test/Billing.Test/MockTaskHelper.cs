using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Test
{
   public class MockTaskHelper : ITaskHelper
    {
        public Task<bool> RetryUntilSuccessOrTimeout(string name, Func<Task<bool>> callback, TimeSpan timeoutTimeSpan, TimeSpan? waitTimeSpan = null, IDiagnosticsLogger logger = null, Action onTimeout = null)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RetryUntilSuccessOrTimeout(string name, Func<IDiagnosticsLogger, Task<bool>> callback, TimeSpan timeoutTimeSpan, TimeSpan? waitTimeSpan = null, IDiagnosticsLogger logger = null, Action onTimeout = null)
        {
            throw new NotImplementedException();
        }

        public void RunBackground(string name, Func<IDiagnosticsLogger, Task> callback, IDiagnosticsLogger logger = null, bool autoLogOperation = true, Action<Exception, IDiagnosticsLogger> errCallback = null, TimeSpan? delay = null)
        {
            throw new NotImplementedException();
        }

        public void RunBackgroundConcurrentEnumerable<T>(string name, IEnumerable<T> list, Func<T, IDiagnosticsLogger, Task> callback, IDiagnosticsLogger logger = null, Func<T, IDiagnosticsLogger, Task<IDisposable>> obtainLease = null, Action<T, Exception, IDiagnosticsLogger> errItemCallback = null, int concurrentLimit = 3, int successDelay = 250)
        {
            callback(list.First(), logger);
        }

        public Task RunConcurrentEnumerableAsync<T>(string name, IEnumerable<T> list, Func<T, IDiagnosticsLogger, Task> callback, IDiagnosticsLogger logger = null, Func<T, IDiagnosticsLogger, Task<IDisposable>> obtainLease = null, Action<T, Exception, IDiagnosticsLogger> errItemCallback = null, int concurrentLimit = 3, int successDelay = 250)
        {
            return callback(list.First(), logger);
        }

        public void RunBackgroundLong(string name, Func<IDiagnosticsLogger, Task> callback, IDiagnosticsLogger logger = null, bool autoLogOperation = true, Action<Exception, IDiagnosticsLogger> errCallback = null, TimeSpan? delay = null)
        {
            throw new NotImplementedException();
        }

        public void RunBackgroundLoop(string name, Func<IDiagnosticsLogger, Task<bool>> callback, TimeSpan? schedule = null, IDiagnosticsLogger logger = null, bool autoLogLoopOperation = false, Func<Exception, IDiagnosticsLogger, bool> errLoopCallback = null)
        {
            throw new NotImplementedException();
        }

        public Task RunBackgroundLoopAsync(string name, Func<IDiagnosticsLogger, Task<bool>> callback, TimeSpan? schedule = null, IDiagnosticsLogger logger = null, bool autoLogLoopOperation = false, Func<Exception, IDiagnosticsLogger, bool> errLoopCallback = null)
        {
            throw new NotImplementedException();
        }

        public void RunBackgroundEnumerable<T>(string name, IEnumerable<T> list, Func<T, IDiagnosticsLogger, Task> callback, IDiagnosticsLogger logger = null, Func<T, IDiagnosticsLogger, Task<IDisposable>> obtainLease = null, int itemDelay = 250)
        {
            throw new NotImplementedException();
        }

        public Task RunEnumerableAsync<T>(string name, IEnumerable<T> list, Func<T, IDiagnosticsLogger, Task> callback, IDiagnosticsLogger logger = null, Func<T, IDiagnosticsLogger, Task<IDisposable>> obtainLease = null, int itemDelay = 250)
        {
            throw new NotImplementedException();
        }
    }
}
