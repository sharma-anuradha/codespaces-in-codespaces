using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repositories
{
    public class CloudEnvironmentLogRecord
    {
        public string SkuName { get; set; }

        public string Location { get; set; }

        public string State { get; set; }

        public int Count { get; set; }
    }
}
