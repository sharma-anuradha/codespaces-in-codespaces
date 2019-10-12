﻿// <copyright file="FakeResourceBrokerClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient.ResourceBroker.Fakes
{
    /// <summary>
    /// Fake resource broker client. Provides local docker instead of azure resources.
    /// </summary>
    public class FakeResourceBrokerClient : IResourceBrokerResourcesHttpContract
    {
        private const string DockerCLI = "docker";

        private readonly ConcurrentDictionary<Guid, ResourceBrokerResource> resources = new ConcurrentDictionary<Guid, ResourceBrokerResource>();
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
        public Task<bool> CleanupResourceAsync(Guid resourceId, string environmentId, IDiagnosticsLogger logger)
        {
            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        public async Task<ResourceBrokerResource> CreateResourceAsync(CreateResourceRequestBody allocateRequestBody, IDiagnosticsLogger logger)
        {
            await Task.CompletedTask;

            var resource = new ResourceBrokerResource
            {
                ResourceId = Guid.NewGuid(),
                Created = DateTime.UtcNow,
                Location = allocateRequestBody.Location,
                SkuName = allocateRequestBody.SkuName,
            };

            if (!resources.TryAdd(resource.ResourceId, resource))
            {
                throw new InvalidOperationException($"Resource already found {resource.ResourceId}");
            }

            return resource;
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteResourceAsync(Guid resourceId, IDiagnosticsLogger logger)
        {
            await Task.CompletedTask;

            var stopDockerContainerProcess = Process.Start("docker", $"stop {resourceId}");
            stopDockerContainerProcess.WaitForExit();

            return resources.Remove(resourceId, out _);
        }

        /// <inheritdoc/>
        public async Task<ResourceBrokerResource> GetResourceAsync(Guid resourceId, IDiagnosticsLogger logger)
        {
            await Task.CompletedTask;

            return resources.GetValueOrDefault(resourceId);
        }

        /// <inheritdoc/>
        public async Task StartComputeAsync(Guid computeResourceId, StartComputeRequestBody startComputeRequestBody, IDiagnosticsLogger logger)
        {
            await Task.CompletedTask;

            _ = CreateDockerContainerWithCopiedCLI(this.dockerImageName, this.publishedCLIPath, startComputeRequestBody, computeResourceId);
        }

        private string CreateDockerContainerWithCopiedCLI(string image, string cliPublishedpath, StartComputeRequestBody startComputeRequestBody, Guid computeResourceId)
        {
            var containerName = computeResourceId.ToString();
            var createCommandLine = new StringBuilder();
            createCommandLine.Append("create ");
            createCommandLine.Append($"{GetCreateOrRunArguments(image, containerName, startComputeRequestBody)} ");
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

                string vsoCliExecutable = Path.Combine(cliPublishedpath, "vsonline");
                if (!File.Exists(vsoCliExecutable))
                {
                    throw new FileNotFoundException($"{vsoCliExecutable} not found. Publish VSO CLI and check the output.");
                }
                // End sanity test

                var dockerCopyCommandLine = new StringBuilder();
                dockerCopyCommandLine.Append("cp ");
                dockerCopyCommandLine.Append($"{cliPublishedpath}/. "); // Added /. to copy the contents of the directory
                dockerCopyCommandLine.Append($"{containerName}:/.vsonline/bin ");
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

        private string GetCreateOrRunArguments(string image, string containerName, StartComputeRequestBody startComputeRequestBody)
        {
            var createCommandLine = new StringBuilder();
            foreach (var env in startComputeRequestBody.EnvironmentVariables)
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
            createCommandLine.Append($"{image} /.vsonline/bin/vso bootstrap");
            return createCommandLine.ToString();
        }
    }
}