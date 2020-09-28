// <copyright file="VMAgentUploader.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.Blob;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.InnerLoop.Services
{
    /// <summary>
    /// Uploads agents.
    /// </summary>
    public static class VMAgentUploader
    {
        /// <summary>
        /// Upload Agents.
        /// </summary>
        /// <param name="services">IServiceProvider settings.</param>
        /// <returns>Updated SkuCatalogSettings.</returns>
        public static async Task<SkuCatalogSettings> ExecuteCommandAsync(IServiceProvider services)
        {
            var defaultContainerName = GetDefaultContainerName(services);
            Console.WriteLine($"Default container name: {defaultContainerName}");

            var controlPlaneResourceAccessor = services.GetRequiredService<IControlPlaneAzureResourceAccessor>();
            var locationEnvVar = System.Environment.GetEnvironmentVariable("AZURE_LOCATION");

            var azureLocation = AzureLocation.WestUs2;
            if (!string.IsNullOrEmpty(locationEnvVar))
            {
                Console.WriteLine($"Location set via Environment Variable: {locationEnvVar}");
                azureLocation = Enum.Parse<AzureLocation>(locationEnvVar, ignoreCase: true);
            }

            var (accountName, accountKey) = await controlPlaneResourceAccessor.GetStampStorageAccountForComputeVmAgentImagesAsync(azureLocation);
            Console.WriteLine($"AccountName: {accountName}");

            var blobStorageClientOptions = new BlobStorageClientOptions
            {
                AccountName = accountName,
                AccountKey = accountKey,
            };

            var blobClientProvider = new BlobStorageClientProvider(Options.Create(blobStorageClientOptions));
            var defaultContainer = blobClientProvider.GetCloudBlobContainer(defaultContainerName);
            var devContainer = blobClientProvider.GetCloudBlobContainer(GetDeveloperContainerName(services));

            await devContainer.CreateIfNotExistsAsync();

            return await UploadDefaults(defaultContainer, devContainer);
        }

        private static async Task<SkuCatalogSettings> UploadDefaults(CloudBlobContainer defaultContainer, CloudBlobContainer devContainer)
        {
            var vmAgents = GetCurrentVMAgents();

            foreach (var item in vmAgents)
            {
                var file = item.Value;
                var cloudBlockBlob = defaultContainer.GetBlockBlobReference(file);
                var devcloudBlockBlob = devContainer.GetBlockBlobReference(file);

                if (await devcloudBlockBlob.ExistsAsync())
                {
                    Console.WriteLine($"{file} already uploaded, continue.");
                    continue;
                }

                using (var ms = new MemoryStream())
                {
                    await cloudBlockBlob.DownloadToStreamAsync(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    await devcloudBlockBlob.UploadFromStreamAsync(ms);
                    Console.WriteLine($"uploaded: {file}");
                }
            }

            return UpdateDeveloperAppConfig(vmAgents);
        }

        private static Dictionary<string, string> GetCurrentVMAgents()
        {
            // assume we are running in the settings directory or the published directory.
            var file = Path.Combine(new DirectoryInfo(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)).FullName, "appsettings.images.json");

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
        private static string GetDefaultContainerName(IServiceProvider services)
        {
            var controlPlane = services.GetRequiredService<IControlPlaneInfo>();

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

        private static string GetDeveloperContainerName(IServiceProvider services)
        {
            var controlPlane = services.GetRequiredService<IControlPlaneInfo>();

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

        private static SkuCatalogSettings UpdateDeveloperAppConfig(Dictionary<string, string> vmAgents)
        {
            var families = new Dictionary<string, ImageFamilySettings>();
            foreach (var item in vmAgents)
            {
                families[item.Key] = new ImageFamilySettings()
                {
                    ImageName = item.Value,
                };
            }

            return new SkuCatalogSettings()
            {
                VmAgentImageFamilies = families,
            };
        }
    }
}
