// <copyright file="DeploymentUtils.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine
{
    /// <summary>
    /// Common Deployment utilities between Linux and Windows.
    /// </summary>
    public static class DeploymentUtils
    {
        private static readonly int MaxLength = 1000;

        /// <summary>
        /// Create exception for ARM deployment failures.
        /// </summary>
        /// <param name="deployment">deployment object.</param>
        /// <returns>Error details for ARM Deployment.</returns>
        public static async Task<string> ExtractDeploymentErrors(IDeployment deployment)
        {
            var operations = await deployment.DeploymentOperations.ListAsync();
            foreach (var op in operations)
            {
                if (op.ProvisioningState == "Failed" && op.TargetResource != null)
                {
                    // Log ResourceId, StatusCode and Status Message
                    var errorMessage = op.StatusMessage.ToString();
                    var startIndex = (errorMessage.Length > MaxLength) ? errorMessage.Length - MaxLength : 0;
                    errorMessage = errorMessage.Substring(startIndex);

                    var errorDetails = new DeploymentErrorDetails()
                    {
                        Id = op.TargetResource?.Id,
                        StatusCode = op.StatusCode,
                        ErrorMessage = errorMessage,
                    };

                    var opDetailsString = JsonConvert.SerializeObject(errorDetails);
                    return opDetailsString;
                }
            }

            // No errors found
            return string.Empty;
        }
    }
}
