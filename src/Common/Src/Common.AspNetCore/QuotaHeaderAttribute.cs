// <copyright file="QuotaHeaderAttribute.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore
{
    /// <summary>
    /// Quota Header Attribute used to decorate controllers/actions that should include
    /// subscription compute quota values in their response headers.
    /// </summary>
    public class QuotaHeaderAttribute : ActionFilterAttribute
    {
        private const string CoreLimitHeader = "x-codespaces-core-limit";
        private const string CoreUsageHeader = "x-codespaces-core-usage";
        private const string LogBaseName = "quota_header_attribute";

        /// <inheritdoc/>
        public override async void OnActionExecuted(ActionExecutedContext context)
        {
            var logger = context.HttpContext.GetLogger().NewChildLogger();
            var controllerActionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
            var controllerName = controllerActionDescriptor?.ControllerName;
            var logName = controllerName + "_" + LogBaseName;

            await logger.OperationScopeAsync(
              $"{logName}_on_action_executed",
              (childLogger) =>
              {
                  var result = (CloudEnvironmentResult)((ObjectResult)context.Result)?.Value;
                  if (result?.SubscriptionData != null)
                  {
                      var computeUsage = result.SubscriptionData.ComputeUsage;
                      var computeQuota = result.SubscriptionData.ComputeQuota;
                      var subscriptionId = result.SubscriptionData.SubscriptionId;
                      if (computeUsage != default)
                      {
                          context.HttpContext.Response.Headers.Add(CoreUsageHeader, computeUsage.ToString());
                      }

                      if (computeQuota != default)
                      {
                          context.HttpContext.Response.Headers.Add(CoreLimitHeader, computeQuota.ToString());
                      }

                      logger.AddValue("SubscriptionId", subscriptionId);
                      logger.AddValue("CurrentComputeUsage", computeUsage.ToString());
                      logger.AddValue("CurrentComputeQuota", computeQuota.ToString());
                  }
                  else
                  {
                      logger.AddErrorDetail("CloudEnvironmentResult was null.");
                      logger.LogError($"{logName}_on_action_executed_error");
                  }

                  return Task.CompletedTask;
              }, swallowException: true);
        }
    }
}
