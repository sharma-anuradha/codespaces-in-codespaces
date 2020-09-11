// <copyright file="FakeResourceBrokerClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient.ResourceBroker.Fakes
{
    /// <summary>
    /// Fake resource broker client. Provides local docker instead of azure resources.
    /// </summary>
    public class FakeResourceBrokerClient : IResourceBrokerResourcesExtendedHttpContract
    {
        private const string DockerCLI = "docker";

        private readonly ConcurrentDictionary<Guid, AllocateResponseBody> resources = new ConcurrentDictionary<Guid, AllocateResponseBody>();
        private readonly string dockerImageName;
        private readonly string publishedCLIPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="FakeResourceBrokerClient"/> class.
        /// </summary>
        /// <param name="dockerImageName">Name of the local docker image.</param>
        /// <param name="publishedCLIPath">Published CLI path.</param>
        public FakeResourceBrokerClient(string dockerImageName, string publishedCLIPath)
        {
            this.dockerImageName = Requires.NotNull(dockerImageName, nameof(dockerImageName));
            this.publishedCLIPath = publishedCLIPath;
        }

        /// <inheritdoc/>
        public Task<ResourceBrokerResource> GetAsync(Guid resourceId, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<AllocateResponseBody> AllocateAsync(Guid environmentId, AllocateRequestBody resource, IDiagnosticsLogger logger)
        {
            var result = new AllocateResponseBody
            {
                ResourceId = Guid.NewGuid(),
                Created = DateTime.UtcNow,
                Location = resource.Location,
                SkuName = resource.SkuName,
            };

            if (!resources.TryAdd(result.ResourceId, result))
            {
                throw new InvalidOperationException($"Resource already found {result.ResourceId}");
            }

            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<AllocateResponseBody>> AllocateAsync(Guid environmentId, IEnumerable<AllocateRequestBody> resources, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null)
        {
            var results = new List<AllocateResponseBody>();
            foreach (var resource in resources)
            {
                results.Add(await AllocateAsync(environmentId, resource, logger));
            }

            return results;
        }

        /// <inheritdoc/>
        public Task<bool> StartAsync(Guid environmentId, StartRequestAction action, StartRequestBody resource, IDiagnosticsLogger logger)
        {
            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        public Task<bool> StartAsync(Guid environmentId, StartRequestAction action, IEnumerable<StartRequestBody> resources, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null)
        {
            if (action == StartRequestAction.StartCompute)
            {
                var backingResources = resources.Select(x => this.resources.Where(y => y.Key == x.ResourceId).Select(y => (Resource: x, Record: y.Value)).Single());
                var computeResource = backingResources.Where(x => x.Record.Type == Common.Contracts.ResourceType.ComputeVM).Single();
                var storageResource = backingResources.Where(x => x.Record.Type == Common.Contracts.ResourceType.StorageFileShare).Single();

                _ = CreateDockerContainerWithCopiedCLI(dockerImageName, publishedCLIPath, computeResource.Resource);
            }

            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        public Task<bool> SuspendAsync(Guid environmentId, Guid resourceId, IDiagnosticsLogger logger)
        {
            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        public Task<bool> SuspendAsync(Guid environmentId, IEnumerable<Guid> resources, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null)
        {
            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        public Task<bool> DeleteAsync(Guid environmentId, Guid resourceId, IDiagnosticsLogger logger)
        {
            var stopDockerContainerProcess = Process.Start("docker", $"stop {resourceId}");
            stopDockerContainerProcess.WaitForExit();

            resources.Remove(resourceId, out _);

            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(Guid environmentId, IEnumerable<Guid> resources, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null)
        {
            foreach (var resource in resources)
            {
                await DeleteAsync(environmentId, resource, logger);
            }

            return true;
        }

        /// <inheritdoc/>
        public Task<StatusResponseBody> StatusAsync(Guid environmentId, Guid resourceId, IDiagnosticsLogger logger)
        {
            return Task.FromResult(new StatusResponseBody { ResourceId = resourceId });
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<StatusResponseBody>> StatusAsync(Guid environmentId, IEnumerable<Guid> resources, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null)
        {
            var result = new List<StatusResponseBody>();
            foreach (var resource in resources)
            {
                result.Add(await StatusAsync(environmentId, resource, logger));
            }

            return result;
        }

        /// <inheritdoc/>
        public Task<bool> ProcessHeartbeatAsync(Guid environmentId, Guid resourceId, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null)
        {
            return Task.FromResult(resources.ContainsKey(resourceId));
        }

        private string CreateDockerContainerWithCopiedCLI(string image, string cliPublishedpath, StartRequestBody computeResource)
        {
            var containerName = computeResource.ResourceId.ToString();
            var createCommandLine = new StringBuilder();
            createCommandLine.Append("create ");
            createCommandLine.Append($"{GetCreateOrRunArguments(image, containerName, computeResource)} ");
            var createDockerProcess = Process.Start(DockerCLI, createCommandLine.ToString());
            createDockerProcess.WaitForExit();
            if (createDockerProcess.ExitCode != 0)
            {
                throw new InvalidOperationException($"MockCompute: docker create failed with error code {createDockerProcess.ExitCode}");
            }

            // Copy the CLI into the container if it's set
            if (cliPublishedpath != null)
            {
                // Sanity test
                if (!Directory.Exists(cliPublishedpath))
                {
                    throw new DirectoryNotFoundException($"{cliPublishedpath} not found. Make sure you publish the cli and point appsettings.Development.json at it.");
                }

                var vsoCliExecutable = Path.Combine(cliPublishedpath, "vso");
                if (!File.Exists(vsoCliExecutable))
                {
                    throw new FileNotFoundException($"{vsoCliExecutable} not found. Publish VSO CLI and check the output.");
                }

                // End sanity test
                var dockerCopyCommandLine = new StringBuilder();
                dockerCopyCommandLine.Append("cp ");
                dockerCopyCommandLine.Append($"{cliPublishedpath}/. "); // Added /. to copy the contents of the directory
                dockerCopyCommandLine.Append($"{containerName}:/.codespaces/bin ");
                var dockerCopyProcess = Process.Start(DockerCLI, dockerCopyCommandLine.ToString());
                dockerCopyProcess.WaitForExit();
                if (dockerCopyProcess.ExitCode != 0)
                {
                    throw new InvalidOperationException($"MockCompute: docker copy command failed with error code {dockerCopyProcess.ExitCode}");
                }
            }

            var dockerStartCommandLine = new StringBuilder();
            dockerStartCommandLine.Append($"start {containerName}");
            var dockerStartProcess = new Process
            {
                EnableRaisingEvents = true,
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

        private string GetCreateOrRunArguments(string image, string containerName, StartRequestBody computeResource)
        {
            var createCommandLine = new StringBuilder();
            foreach (var env in computeResource.Variables)
            {
                if (env.Key == "SESSION_CALLBACK")
                {
                    // Need to access the host of the docker container via host.docker.internal to reach the locally running frontend.
                    var callback = env.Value.Replace("localhost", "host.docker.internal");
                    createCommandLine.Append($"-e{env.Key}=\"{callback}\" ");
                }
                else
                {
                    createCommandLine.Append($"-e{env.Key}=\"{env.Value}\" ");
                }
            }

            createCommandLine.Append($"--name {containerName} ");
            createCommandLine.Append($"{image} /.codespaces/bin/vso bootstrap");
            return createCommandLine.ToString();
        }
    }
}