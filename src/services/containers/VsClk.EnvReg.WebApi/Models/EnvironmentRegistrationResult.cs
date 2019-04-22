using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VsCloudKernel.Services.EnvReg.Models
{
    public class EnvironmentRegistrationResult
    {
        public string Id { get; set; }

        public string OwnerId { get; set; }

        public string FriendlyName { get; set; }

        public DateTime Created { get; set; }

        public DateTime Updated { get; set; }

        public DateTime Active { get; set; }
    }
}