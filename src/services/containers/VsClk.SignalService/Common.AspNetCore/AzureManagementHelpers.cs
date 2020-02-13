// <copyright file="AzureManagementHelpers.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Helpers for the Azure Management Fluent
    /// </summary>
    public static class AzureManagementHelpers
    {
        public static bool IsDefined(
            this ApplicationServicePrincipal applicationServicePrincipal)
        {
            return applicationServicePrincipal != null &&
                !(string.IsNullOrEmpty(applicationServicePrincipal.ClientId) ||
                string.IsNullOrEmpty(applicationServicePrincipal.ClientPassword) ||
                string.IsNullOrEmpty(applicationServicePrincipal.TenantId));
        }

        public static AzureCredentials GetAzureCredentials(this ApplicationServicePrincipal applicationServicePrincipal)
        {
            return new AzureCredentialsFactory()
                .FromServicePrincipal(
                    applicationServicePrincipal.ClientId,
                    applicationServicePrincipal.ClientPassword,
                    applicationServicePrincipal.TenantId,
                    AzureEnvironment.AzureGlobalCloud);
        }
    }
}
