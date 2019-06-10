using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.Services.EnvReg.Models
{
    public class EnvironmentRegistrationCallbackInput
    {
        [Required]
        public string Type { get; set; }

        public EnvironmentRegistrationCallbackPayloadInput Payload { get; set; }
    }

    public class EnvironmentRegistrationCallbackPayloadInput
    {
        public string SessionId { get; set; }

        public string SessionPath { get; set; }
    }
}
