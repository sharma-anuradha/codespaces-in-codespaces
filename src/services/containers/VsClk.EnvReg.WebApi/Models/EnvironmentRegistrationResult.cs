using System;
using Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore;

namespace Microsoft.VsCloudKernel.Services.EnvReg.Models
{
    public class EnvironmentRegistrationResult
    {
        public string Id { get; set; }

        public string Type { get; set; }

        public string FriendlyName { get; set; }

        public DateTime Created { get; set; }

        public DateTime Updated { get; set; }

        public string OwnerId { get; set; }

        public string State { get; set; }

        public string ContainerImage { get; set; }

        public SeedInfo Seed { get; set; }

        public ConnectionInfo Connection { get; set; }

        public DateTime Active { get; set; }

    }
}