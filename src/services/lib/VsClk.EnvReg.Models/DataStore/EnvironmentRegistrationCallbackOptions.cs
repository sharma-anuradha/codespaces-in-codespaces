using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore
{
    public class EnvironmentRegistrationCallbackOptions
    {
        public string Type { get; set; }

        public EnvironmentRegistrationCallbackPayloadOptions Payload { get; set; }
    }

    public class EnvironmentRegistrationCallbackPayloadOptions
    {
        public string SessionId { get; set; }

        public string SessionPath { get; set; }
    }
}
