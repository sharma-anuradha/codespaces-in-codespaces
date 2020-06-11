using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Subscriptions.Test
{
    public class MockCrossRegionContinuationTaskActivator : ICrossRegionContinuationTaskActivator
    {
        public Task<ContinuationResult> ExecuteForDataPlane(
            string name, 
            AzureLocation dataPlaneRegion, 
            ContinuationInput input, 
            IDiagnosticsLogger logger, 
            Guid? systemId = null, 
            IDictionary<string, string> loggerProperties = null)
        {
            throw new NotImplementedException();
        }
    }
}
