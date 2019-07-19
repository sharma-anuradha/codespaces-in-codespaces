using Microsoft.VsCloudKernel.Services.EnvReg.Models;
using Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using VsClk.EnvReg.Models.DataStore.Compute;
using VsClk.EnvReg.Repositories;

namespace Microsoft.VsCloudKernel.Services.EnvReg.Repositories
{
#if DEBUG

    public class MockACI
    {
        public ComputeServiceRequest ComputeServiceRequest
        {
            get;
            set;
        }

        public ComputeResourceResponse ComputeResourceResponse
        {
            get;
            set;
        }

        public string ContainerInstance
        {
            get;
            set;
        }
    }

    public class MockComputeRepository : IComputeRepository
    {
        // Map of computeId to request and response.
        private readonly Dictionary<string, MockACI> store = new Dictionary<string, MockACI>();
        private AppSettings appSettings;

        public MockComputeRepository(AppSettings appSettings)
        {
            this.appSettings = appSettings;
        }

        public Task<ComputeResourceResponse> AddResourceAsync(string computeTargetId, ComputeServiceRequest computeServiceRequest)
        {
            string containerInstance;
            if (appSettings.UseLocalDockerForComputeProvisioning)
            {
                containerInstance = CreateDockerContainer(appSettings.DockerImage, appSettings.PublishedCLIPath, computeServiceRequest);
            }
            else
            {
                containerInstance = Guid.NewGuid().ToString();
            }

            var computeResource = new ComputeResourceResponse()
            {
                Created = DateTime.Now.ToString(),
                Id = containerInstance,
                State = StateInfo.Provisioning.ToString()
            };

            this.store[computeResource.Id] = new MockACI()
            {
                ComputeServiceRequest = computeServiceRequest,
                ComputeResourceResponse = computeResource,
                ContainerInstance = containerInstance
            };

            return Task.FromResult(computeResource);
        }

        public Task DeleteResourceAsync(string connectionComputeTargetId, string connectionComputeId)
        {
            if (appSettings.UseLocalDockerForComputeProvisioning)
            {
                var stopDockerContainerProcess = Process.Start("docker", $"stop {connectionComputeId}");
                stopDockerContainerProcess.WaitForExit();
            }

            this.store.Remove(connectionComputeId);
            return Task.CompletedTask;
        }

        public Task<List<ComputeTargetResponse>> GetTargetsAsync()
        {
            var result = new List<ComputeTargetResponse>();

            result.Add(new ComputeTargetResponse()
            {
                Created = DateTime.Now.ToString(),
                Id = Guid.NewGuid().ToString(),
                Name = "Mock",
                Properties = new Dictionary<string, dynamic>() { { "region", null } },
                State = "Available",
            });

            return Task.FromResult(result);
        }

        private string CreateDockerContainer(string image, string cliPublishedpath, ComputeServiceRequest computeServiceRequest)
        {
            var containerName = Guid.NewGuid().ToString();
            var commandLine = new StringBuilder();
            commandLine.Append("run ");
            commandLine.Append($"-v {cliPublishedpath}:/.cloudenv/bin ");
            foreach (var env in computeServiceRequest.EnvironmentVariables)
            {
                if (env.Key == "SESSION_CALLBACK")
                {
                    // Instead of doing the callback to https://online.dev.core.vsengsaas.visualstudio.com/api/environment/registration/25ad9677-dcc8-4889-8e13-73c5b61e3a2b/_callback
                    // do callback to local http://localhost:62055/api/registration/{id}/_callback

                    var callback = env.Value.Replace("https://online.dev.core.vsengsaas.visualstudio.com/api/environment/registration/", this.appSettings.LocalEnvironmentServiceUrl);
                    commandLine.Append($"-e{env.Key}=\"{callback}\" ");
                }
                else
                {
                    commandLine.Append($"-e{env.Key}=\"{env.Value}\" ");
                }
            }

            commandLine.Append($"--name {containerName} ");
            commandLine.Append($"{image} /.cloudenv/bin/vscloudenv bootstrap");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo("docker", commandLine.ToString())
            };

            process.Exited += (s,e) =>
            {
                process.Dispose();
                process = null;
            };

            process.Start();
            return containerName;
        }
    }

#endif // DEBUG
}
