// <copyright file="PrepareDevCLICommand.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.Blob;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil;
using Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil.Models;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil
{
    /// <summary>
    /// Copies the CLIs to a devstamp storage container to be used for the devstamp.
    /// </summary>
    [Verb("preparedevcli", HelpText = "Prepare a dev CLI for dev stamp.")]
    public class PrepareDevCLICommand : CommandBase
    {
        /// <summary>
        /// Gets or sets the custom cli path location.
        /// </summary>
        [Option('c', "customcli", Default = null, HelpText = "Path to custom CLI zip files.")]
        public string CustomCLIPath { get; set; }

        /// <summary>
        /// Gets or sets the custom cli version.
        /// </summary>
        [Option('v', "version", Default = null, HelpText = "CLI version.")]
        public string CustomCLIVersion { get; set; }

        /// <summary>
        /// Gets or sets the data plane location.
        /// </summary>
        [Option('d', "dataplane", Default = "WestUs2", HelpText = "Data Plane Location. Defaults to WestUs2.")]
        public string DataPlaneLocation { get; set; }

        /// <summary>
        /// Gets or sets the forwarding host name.
        /// </summary>
        [Option('f', "forwardingHostName", Default = null, HelpText = "forwarding host name. ngrok hostname goes here.")]
        public string ForwardingHostForLocalDevelopment { get; set; }

        /// <inheritdoc/>
        protected override void ExecuteCommand(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            ExecuteCommandAsync(services, stdout).Wait();
        }

        private async Task ExecuteCommandAsync(IServiceProvider services, TextWriter stdout)
        {
            var defaultContainerName = GetDefaultContainerName(services);
            stdout.WriteLine($"Default container name: {defaultContainerName}");

            var controlPlane = GetControlPlaneInfo();
            var controlPlaneResourceAccessor = services.GetRequiredService<IControlPlaneAzureResourceAccessor>();
            var azureLocation = Enum.Parse<AzureLocation>(DataPlaneLocation, ignoreCase: true);
            var location = controlPlane.GetAllDataPlaneLocations().FirstOrDefault(n => n == azureLocation);
            var (accountName, accountKey) = await controlPlaneResourceAccessor.GetStampStorageAccountForComputeVmAgentImagesAsync(location);
            stdout.WriteLine($"AccountName: {accountName}");

            var blobStorageClientOptions = new BlobStorageClientOptions
            {
                AccountName = accountName,
                AccountKey = accountKey,
            };

            var blobClientProvider = new BlobStorageClientProvider(Options.Create(blobStorageClientOptions));
            var defaultContainer = blobClientProvider.GetCloudBlobContainer(defaultContainerName);
            var devContainer = blobClientProvider.GetCloudBlobContainer(GetDeveloperContainerName(services));

            await devContainer.CreateIfNotExistsAsync();

            if (string.IsNullOrWhiteSpace(CustomCLIPath))
            {
                await UploadDefaults(defaultContainer, devContainer, stdout);
            }
            else
            {
                await UploadDevCLI(devContainer, stdout);
            }
        }

        private void UpdateDeveloperAppConfig(Dictionary<string, string> vmAgents)
        {
            var devConfigFile = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), "CEDev", "appsettings.json");
            var devConfig = JsonConvert.DeserializeObject<AppConfiguration>(File.ReadAllText(devConfigFile));

            var families = new Dictionary<string, ImageFamilySettings>();
            foreach (var item in vmAgents)
            {
                families[item.Key] = new ImageFamilySettings()
                {
                    ImageName = item.Value,
                };
            }

            devConfig.AppSettings.SkuCatalogSettings = new Models.SkuCatalogSettings()
            {
                VmAgentImageFamilies = families,
            };

            if (!string.IsNullOrWhiteSpace(ForwardingHostForLocalDevelopment))
            {
                devConfig.AppSettings.FrontEnd.ForwardingHostForLocalDevelopment = ForwardingHostForLocalDevelopment;
                devConfig.AppSettings.ControlPlaneSettings.DnsHostName = ForwardingHostForLocalDevelopment;
                foreach (var stamp in devConfig.AppSettings.ControlPlaneSettings.Stamps)
                {
                    stamp.Value.DnsHostName = ForwardingHostForLocalDevelopment;
                }
            }

            File.WriteAllText(devConfigFile, JsonConvert.SerializeObject(devConfig, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
            }));
        }

        private async Task UploadDefaults(CloudBlobContainer defaultContainer, CloudBlobContainer devContainer, TextWriter stdout)
        {
            var vmAgents = GetCurrentVMAgents();

            foreach (var item in vmAgents)
            {
                var file = item.Value;
                var cloudBlockBlob = defaultContainer.GetBlockBlobReference(file);
                var devcloudBlockBlob = devContainer.GetBlockBlobReference(file);

                using (var ms = new MemoryStream())
                {
                    await cloudBlockBlob.DownloadToStreamAsync(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    await devcloudBlockBlob.UploadFromStreamAsync(ms);
                    stdout.WriteLine($"uploaded: {file}");
                }
            }

            stdout.WriteLine($"Updating CEDev config file.");
            UpdateDeveloperAppConfig(vmAgents);
        }

        private async Task UploadDevCLI(CloudBlobContainer devContainer, TextWriter stdout)
        {
            var files = Directory.EnumerateFiles(CustomCLIPath, "*.zip");
            if (!string.IsNullOrWhiteSpace(CustomCLIVersion))
            {
                files = files.Where(x => x.EndsWith($"{CustomCLIVersion}.zip"));
            }

            var vmAgents = new Dictionary<string, string>();

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);

                if (fileName.Contains("_linux_"))
                {
                    vmAgents["vsoagentlinux"] = fileName;
                }
                else if (fileName.Contains("_win_"))
                {
                    vmAgents["vsoagentwin"] = fileName;
                }
                else if (fileName.Contains("_osx_"))
                {
                    vmAgents["vsoagentosx"] = fileName;
                }
                else
                {
                    throw new InvalidDataException("Unexpected file.");
                }

                var devcloudBlockBlob = devContainer.GetBlockBlobReference(fileName);
                await devcloudBlockBlob.UploadFromFileAsync(file);
                stdout.WriteLine($"Uploaded file {file}");
            }

            stdout.WriteLine($"Updating CEDev config file.");
            UpdateDeveloperAppConfig(vmAgents);
        }

        private Dictionary<string, string> GetCurrentVMAgents()
        {
            // assume we are running in the settings directory or the published directory.
            var file = "appsettings.images.json";

            dynamic imagesObj = JObject.Parse(File.ReadAllText(file));
            var agentFamilies = imagesObj.AppSettings.skuCatalogSettings.vmAgentImageFamilies;

            var vmAgents = new Dictionary<string, string>
            {
                ["vsoagentlinux"] = agentFamilies.vsoagentlinux.imageName,
                ["vsoagentwin"] = agentFamilies.vsoagentwin.imageName,
                ["vsoagentosx"] = agentFamilies.vsoagentosx.imageName,
            };

            return vmAgents;
        }

        // TODO: janraj, hack.. need to figure out how to not use dev stamp.?!
        private string GetDefaultContainerName(IServiceProvider services)
        {
            var controlPlane = GetControlPlaneInfo();

            var developerStamp = services.GetRequiredService<DeveloperPersonalStampSettings>();
            if (developerStamp.DeveloperStamp)
            {
                return controlPlane.VirtualMachineAgentContainerName.Replace($"-{System.Environment.UserName}", string.Empty);
            }
            else
            {
                return controlPlane.VirtualMachineAgentContainerName;
            }
        }

        private string GetDeveloperContainerName(IServiceProvider services)
        {
            var controlPlane = GetControlPlaneInfo();

            var developerStamp = services.GetRequiredService<DeveloperPersonalStampSettings>();
            if (developerStamp.DeveloperStamp)
            {
                return controlPlane.VirtualMachineAgentContainerName;
            }
            else
            {
                return $"{controlPlane.VirtualMachineAgentContainerName}-{System.Environment.UserName}";
            }
        }
    }
}
