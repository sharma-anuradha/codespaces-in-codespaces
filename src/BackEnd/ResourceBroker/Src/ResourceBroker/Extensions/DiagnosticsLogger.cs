// <copyright file="DiagnosticsLoggerExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using System;
using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions
{
    public static class DiagnosticsLoggerExtensions
    {
        public static IDiagnosticsLogger AddTaskId(this IDiagnosticsLogger logger, Guid taskId)
        {
            logger.AddValue("taskId", taskId.ToString());
            return logger;
        }

        public static IDiagnosticsLogger AddIterationId(this IDiagnosticsLogger logger, int iterationId)
        {
            logger.AddValue("iterationId", iterationId.ToString());
            return logger;
        }

        public static IDiagnosticsLogger AddResourceLocation(this IDiagnosticsLogger logger, string location)
        {
            logger.AddValue("resourceLocation", location);
            return logger;
        }

        public static IDiagnosticsLogger AddResourceSku(this IDiagnosticsLogger logger, string sku)
        {
            logger.AddValue("resourceSku", sku);
            return logger;
        }

        public static IDiagnosticsLogger AddResourceType(this IDiagnosticsLogger logger, ResourceType type)
        {
            logger.AddValue("resourceType", type.ToString());
            return logger;
        }

        public static IDiagnosticsLogger AddProperties(this IDiagnosticsLogger logger, IDictionary<string, object> properties)
        {
            if (properties != null)
            {
                foreach (var item in properties)
                {
                    logger.AddValue(item.Key, item.Value?.ToString());
                }
            }

            return logger;
        }
    }
}
