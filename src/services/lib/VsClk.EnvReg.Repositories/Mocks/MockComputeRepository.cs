using Microsoft.VsCloudKernel.Services.EnvReg.Models;
using Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

    public enum LocalDockerOptions
    {
        None,
        BindMountCLI,
        CopyCLI
    }

    public class MockComputeRepository : IComputeRepository
    {
        // Map of computeId to request and response.
        private readonly Dictionary<string, MockACI> store = new Dictionary<string, MockACI>();
        private AppSettings appSettings;
        private const string DockerCLI = "docker";

        public MockComputeRepository(AppSettings appSettings)
        {
            this.appSettings = appSettings;
        }

        public Task<ComputeResourceResponse> AddResourceAsync(string computeTargetId, ComputeServiceRequest computeServiceRequest)
        {
            string containerInstance = Guid.NewGuid().ToString();

            Enum.TryParse<LocalDockerOptions>(appSettings.UseLocalDockerForComputeProvisioning, true, out LocalDockerOptions options);
            switch (options)
            {
                case LocalDockerOptions.BindMountCLI:
                    containerInstance = CreateDockerContainerWithBindMount(appSettings.DockerImage, appSettings.PublishedCLIPath, computeServiceRequest);
                    break;
                case LocalDockerOptions.CopyCLI:
                    containerInstance = CreateDockerContainerWithCopiedCLI(appSettings.DockerImage, appSettings.PublishedCLIPath, computeServiceRequest);
                    break;
                case LocalDockerOptions.None:
                default:
                    break;
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
            Enum.TryParse<LocalDockerOptions>(appSettings.UseLocalDockerForComputeProvisioning, true, out LocalDockerOptions options);
            if (options == LocalDockerOptions.CopyCLI || options == LocalDockerOptions.BindMountCLI)
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

        private string CreateDockerContainerWithBindMount(string image, string cliPublishedpath, ComputeServiceRequest computeServiceRequest)
        {
            var containerName = Guid.NewGuid().ToString();
            var commandLine = new StringBuilder();
            commandLine.Append("run ");
            commandLine.Append($"-v {cliPublishedpath}:/.cloudenv/bin ");
            commandLine.Append($"{GetCreateOrRunArguments(image, containerName, computeServiceRequest)} ");
            Process.Start(DockerCLI, commandLine.ToString());

            return containerName;
        }

        private string CreateDockerContainerWithCopiedCLI(string image, string cliPublishedpath, ComputeServiceRequest computeServiceRequest)
        {
            var containerName = Guid.NewGuid().ToString();
            var createCommandLine = new StringBuilder();
            createCommandLine.Append("create ");
            createCommandLine.Append($"{GetCreateOrRunArguments(image, containerName, computeServiceRequest)} ");
            var createDockerProcess = Process.Start(DockerCLI, createCommandLine.ToString());
            createDockerProcess.WaitForExit();
            if (createDockerProcess.ExitCode != 0)
            {
                throw new InvalidOperationException($"MockCompute: docker create failed with error code {createDockerProcess.ExitCode}");
            }

            // Sanity test
            if (!Directory.Exists(cliPublishedpath))
            {
                throw new DirectoryNotFoundException($"{cliPublishedpath} not found. Make sure you publish the cli and point appsettings.Development.json at it.");
            }

            string cloudEnvironmentsCliExecutable = Path.Combine(cliPublishedpath, "vscloudenv");
            if (!File.Exists(cloudEnvironmentsCliExecutable))
            {
                throw new FileNotFoundException($"{cloudEnvironmentsCliExecutable} not found. Publish CloudEnvironments CLI and check the output.");
            }
            // End sanity test

            var dockerCopyCommandLine = new StringBuilder();
            dockerCopyCommandLine.Append("cp ");
            dockerCopyCommandLine.Append($"{cliPublishedpath}/. "); // Added /. to copy the contents of the directory
            dockerCopyCommandLine.Append($"{containerName}:/.cloudenv/bin ");
            var dockerCopyProcess = Process.Start(DockerCLI, dockerCopyCommandLine.ToString());
            dockerCopyProcess.WaitForExit();
            if (dockerCopyProcess.ExitCode != 0)
            {
                throw new InvalidOperationException($"MockCompute: docker copy command failed with error code {dockerCopyProcess.ExitCode}");
            }

            var dockerStartCommandLine = new StringBuilder();
            dockerStartCommandLine.Append($"start {containerName}");
            var dockerStartProcess = new Process
            {
                EnableRaisingEvents = true
            };

            dockerStartProcess.StartInfo = new ProcessStartInfo(DockerCLI, dockerStartCommandLine.ToString());
            dockerStartProcess.Exited += (s, e) =>
            {
                if (dockerStartProcess.ExitCode != 0)
                {
                    throw new InvalidOperationException($"MockCompute: docker exited with error code {dockerStartProcess.ExitCode}");
                }
            };

            dockerStartProcess.Start();

            return containerName;
        }

        private string GetCreateOrRunArguments(string image, string containerName, ComputeServiceRequest computeServiceRequest)
        {
            var createCommandLine = new StringBuilder();
            foreach (var env in computeServiceRequest.EnvironmentVariables)
            {
                if (env.Key == "SESSION_CALLBACK")
                {
                    // Instead of doing the callback to https://online.dev.core.vsengsaas.visualstudio.com/api/environment/registration/25ad9677-dcc8-4889-8e13-73c5b61e3a2b/_callback
                    // do callback to local http://localhost:62055/api/registration/{id}/_callback

                    var callback = env.Value.Replace("https://online.dev.core.vsengsaas.visualstudio.com/api/environment/registration/", this.appSettings.LocalEnvironmentServiceUrl);
                    createCommandLine.Append($"-e{env.Key}=\"{callback}\" ");
                }
                else
                {
                    createCommandLine.Append($"-e{env.Key}=\"{env.Value}\" ");
                }
            }

            createCommandLine.Append($"--name {containerName} ");
            createCommandLine.Append($"{image} /.cloudenv/bin/vscloudenv bootstrap");
            return createCommandLine.ToString();
        }

        public Task<ComputeResourceResponse> RefreshResourceAsync(string computeTargetId, string oldContainerName, ComputeServiceRequest computeServiceRequest)
        {
            if (!this.store.ContainsKey(oldContainerName))
            {
                throw new KeyNotFoundException();
            }

            Enum.TryParse<LocalDockerOptions>(appSettings.UseLocalDockerForComputeProvisioning, true, out LocalDockerOptions options);
            if (options != LocalDockerOptions.CopyCLI)
            {
                throw new NotImplementedException();
            }

            var newImageName = Guid.NewGuid().ToString();
            var newContainerName = Guid.NewGuid().ToString();

            var commitCommand = $"commit {oldContainerName} {newImageName}";
            Process.Start(DockerCLI, commitCommand.ToString()).WaitForExit();
            
            var createCommandLine = new StringBuilder();
            createCommandLine.Append("create ");
            createCommandLine.Append($"{GetCreateOrRunArguments(newImageName, newContainerName, computeServiceRequest)} ");
            Process.Start(DockerCLI, createCommandLine.ToString()).WaitForExit();

            var dockerStartCommandLine = new StringBuilder();
            dockerStartCommandLine.Append($"start {newContainerName}");
            Process.Start(DockerCLI, dockerStartCommandLine.ToString());

            var computeResource = new ComputeResourceResponse()
            {
                Created = DateTime.Now.ToString(),
                Id = newContainerName,
                State = StateInfo.Provisioning.ToString()
            };

            this.store[computeResource.Id] = new MockACI()
            {
                ComputeServiceRequest = computeServiceRequest,
                ComputeResourceResponse = computeResource,
                ContainerInstance = newContainerName
            };

            this.store.Remove(oldContainerName);
            return Task.FromResult(computeResource);
        }
    }

#endif // DEBUG
}
