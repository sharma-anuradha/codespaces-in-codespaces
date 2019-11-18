// <copyright file="SubscriptionsController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Middleware;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Controllers
{
    /// <summary>
    /// The VSO Plan api called by RPSaaS.
    /// </summary>
    [ApiController]
    [Authorize(Policy = "RPSaaSIdentity", AuthenticationSchemes = AuthenticationBuilderRPSaasExtensions.AuthenticationScheme)]
    [FriendlyExceptionFilter]
    [Route(ServiceConstants.ApiV1Route)]
    [LoggingBaseName(LoggingBaseName)]
    public class SubscriptionsController : ControllerBase
    {
        private const string LoggingBaseName = "subscriptions_controller";
        private readonly IPlanManager planManager;
        private readonly ICurrentUserProvider currentUserProvider;
        private readonly ICloudEnvironmentManager cloudEnvironmentManager;
        private const string PlanResourceType = "plans";
        private const string ResourceType = "Microsoft.VSOnline";

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionsController"/> class.
        /// </summary>
        public SubscriptionsController(
            IPlanManager planManager, 
            ICurrentUserProvider currentUserProvider,
            ICloudEnvironmentManager cloudEnvironmentManager)

        {
            this.planManager = planManager;
            this.currentUserProvider = currentUserProvider;
            this.cloudEnvironmentManager = cloudEnvironmentManager;
        }

        /// <summary>
        /// This method will be called by RPSaaS before they create the resource in their DB to validate inputs.
        /// </summary>
        /// <returns>Returns a Http status code and message object indication success or failure of the validation.</returns>
        [HttpPost("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}/resourceCreationValidate")]
        public Task<IActionResult> PlanCreateValidateAsync(
            string subscriptionId,
            string resourceGroup,
            string providerNamespace,
            string resourceType,
            string resourceName,
            [FromBody] PlanResource resource)
        {
            return HttpContext.HttpScopeAsync<IActionResult>(
                $"{LoggingBaseName}_plan_create_validate",
                async (logger) =>
                {
                    try
                    {
                        ValidationUtil.IsRequired(subscriptionId);
                        ValidationUtil.IsRequired(resourceGroup);
                        ValidationUtil.IsRequired(providerNamespace);
                        ValidationUtil.IsRequired(resourceType);
                        ValidationUtil.IsRequired(resourceName);
                        ValidationUtil.IsRequired(resource.Properties?.UserId);
                        ValidationUtil.IsTrue(ResourceTypeIsValid(resourceType));
                        ValidationUtil.IsTrue(ResourceProviderIsValid(providerNamespace));
                    }
                    catch (Exception ex)
                    {
                        logger.LogErrorWithDetail("plan_create_validate_error", ex.Message);
                        return CreateErrorResponse("NullParameters");
                    }

                    if (!await this.planManager.IsPlanCreationAllowedAsync(subscriptionId, logger))
                    {
                        logger.LogErrorWithDetail("plan_create_validate_error", "Plan creation is not allowed.");
                        return CreateErrorResponse("ValidateResourceFailed");
                    }

                    if (resource.Properties != null && resource.Properties.UserId != null)
                    {
                        // TODO: Validate that the user profile exists.
                    }

                    logger.LogInfo("plan_create_validate_success");

                    // Required response format in case validation pass with empty body.
                    return Ok();
                });
        }

        /// <summary>
        /// This method will be called by RPSaaS Service before they create the resource in their DB.
        /// </summary>
        /// <returns>Returns an Http status code and a VSOAccount object.</returns>
        [HttpPut("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}")]
        public Task<IActionResult> PlaneCreateAsync(
            string subscriptionId,
            string resourceGroup,
            string providerNamespace,
            string resourceType,
            string resourceName,
            [FromBody] PlanResource resource)
        {
            return HttpContext.HttpScopeAsync<IActionResult>(
                $"{LoggingBaseName}_plan_create",
                async (logger) =>
                {
                    var accessToken = this.currentUserProvider.GetBearerToken();

                    ValidationUtil.IsRequired(subscriptionId);
                    ValidationUtil.IsRequired(resourceGroup);
                    ValidationUtil.IsRequired(providerNamespace);
                    ValidationUtil.IsRequired(resourceType);
                    ValidationUtil.IsRequired(resourceName);
                    ValidationUtil.IsRequired(resource);
                    var nospacesLocation = resource.Location.Replace(" ", string.Empty);
                    ValidationUtil.IsTrue(
                        Enum.TryParse(nospacesLocation, true, out AzureLocation location),
                        $"Invalid location: ${resource.Location}");
                    ValidationUtil.IsRequired(resource.Properties?.UserId);
                    ValidationUtil.IsTrue(ResourceTypeIsValid(resourceType));
                    ValidationUtil.IsTrue(ResourceProviderIsValid(providerNamespace));

                    var plan = new VsoPlan
                    {
                        Plan = new VsoPlanInfo
                        {
                            Location = location,
                            Name = resourceName,
                            ResourceGroup = resourceGroup,
                            Subscription = subscriptionId,
                        },
                        UserId = resource.Properties.UserId,
                    };

                    await this.planManager.CreateOrUpdateAsync(plan, logger);

                    // Clear the userId property so it will not be stored on the created ARM resource.
                    // It will only be saved internally by the plan manager.
                    resource.Properties.UserId = null;

                    logger.AddVsoPlan(plan.Plan);

                    // Required response format.
                    return CreateResponse(HttpStatusCode.OK, resource);
                },
                (ex, logger) => CreateErrorResponse("CreateResourceFailed"),
                swallowException: true);
        }

        /// <summary>
        /// This method will be called by RPSaaS Service after they create the resource in their DB.
        /// This method could be used to start billing.
        /// </summary>
        /// <returns>Returns a Http status code and message.</returns>
        [HttpPost("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}/resourceCreationCompleted")]
        public Task<IActionResult> PlanCreateComplete(
            string subscriptionId,
            string resourceGroup,
            string providerNamespace,
            string resourceType,
            string resourceName)
        {
            return HttpContext.HttpScopeAsync(
                $"{LoggingBaseName}_plan_create_complete",
                (logger) =>
                {
                    // Do post creation processing here ex: start billing, write billing Events
                    // Required response format with empty body.
                    return Task.FromResult((IActionResult)Ok());
                });
        }

        /// <summary>
        /// This method will be called by RPSaaS Service before they delete the resource in their DB.
        /// </summary>
        /// <returns>Returns a Http status code and message.</returns>
        [HttpPost("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}/resourceDeletionValidate")]
        public Task<IActionResult> PlanDeleteValidate(
            string subscriptionId,
            string resourceGroup,
            string providerNamespace,
            string resourceType,
            string resourceName)
        {
            return HttpContext.HttpScopeAsync<IActionResult>(
                $"{LoggingBaseName}_plan_delete_validate",
                async (logger) =>
                {
                    ValidationUtil.IsRequired(subscriptionId);
                    ValidationUtil.IsRequired(resourceGroup);
                    ValidationUtil.IsRequired(providerNamespace);
                    ValidationUtil.IsRequired(resourceType);
                    ValidationUtil.IsRequired(resourceName);
                    ValidationUtil.IsTrue(ResourceTypeIsValid(resourceType));
                    ValidationUtil.IsTrue(ResourceProviderIsValid(providerNamespace));

                    var plan = new VsoPlanInfo
                    {
                        Name = resourceName,
                        ResourceGroup = resourceGroup,
                        Subscription = subscriptionId,
                    };

                    var environments = await cloudEnvironmentManager.ListEnvironmentsAsync(
                        ownerId: null, name: null, plan.ResourceId, logger);
                    var nonDeletedEnvironments = environments.Where(t => t.State != CloudEnvironmentState.Deleted).ToList();
                    if (nonDeletedEnvironments.Any())
                    {
                        foreach (var environment in nonDeletedEnvironments)
                        {
                            var childLogger = logger.NewChildLogger()
                                .AddVsoPlan(plan);

                            _ = Task.Run(async () =>
                                {
                                      try
                                      {
                                          var result = await cloudEnvironmentManager.DeleteEnvironmentAsync(environment.Id, environment.OwnerId, childLogger);
                                          if (!result)
                                          {
                                              childLogger.AddCloudEnvironment(environment)
                                                .LogError("plan_delete_environment_delete_error");
                                          }
                                      }
                                      catch (Exception ex)
                                      {
                                          childLogger.LogErrorWithDetail("plan_delete_environment_delete_error", ex.Message);
                                      }
                                });
                            }

                        logger.AddVsoPlan(plan)
                            .FluentAddValue("Count", $"{nonDeletedEnvironments.Count()}")
                            .LogInfo("plan_delete_environment_delete_success");
                    }

                    var response = await this.planManager.DeleteAsync(plan, logger);
                    if (!response)
                    {
                        logger.AddVsoPlan(plan).LogError("plan_delete_doesnotexist_error");
                        return Ok();
                    }

                    logger.LogInfo($"plan_delete_success");

                    // Required response format in case validation pass with empty body.
                    return Ok();
                },
                (ex, logger) => CreateErrorResponse("DeleteFailed"),
                swallowException: true);
        }

        /// <summary>
        /// Gets a list of VSO Plan objects filtered by the input subscriptionID and resourceGroup.
        /// </summary>
        /// <returns>Returns an Http status code and a VSOAccount object.</returns>
        [HttpGet("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}")]
        public Task<IActionResult> PlanListAsync(
            string subscriptionId,
            string resourceGroup,
            string providerNamespace,
            string resourceType)
        {
            return HttpContext.HttpScopeAsync<IActionResult>(
                $"{LoggingBaseName}_plan_list",
                async (logger) =>
                {
                    var accessToken = this.currentUserProvider.GetBearerToken();

                    ValidationUtil.IsRequired(subscriptionId);
                    ValidationUtil.IsRequired(resourceGroup);
                    ValidationUtil.IsRequired(providerNamespace);
                    ValidationUtil.IsRequired(resourceType);
                    ValidationUtil.IsTrue(ResourceTypeIsValid(resourceType));
                    ValidationUtil.IsTrue(ResourceProviderIsValid(providerNamespace));

                    var plans = await this.planManager.ListAsync(
                        userId: null, subscriptionId, resourceGroup, logger);

                    logger.LogInfo("plan_list_by_resourcegroup_success");

                    // Required response format.
                    return CreateResponse(HttpStatusCode.OK, plans);
                },
                (ex, logger) => CreateErrorResponse("GetResourceListFailed"),
                swallowException: true);
        }

        /// <summary>
        /// Gets a list of VSO Plan objects filtered by the input subscriptionID.
        /// </summary>
        /// <returns>Returns an Http status code and a list of VSO SkuPlan objects filtering by subscriptionID.</returns>
        [HttpGet("{subscriptionId}/providers/{providerNamespace}/{resourceType}")]
        public Task<IActionResult> PlanListBySubscriptionAsync(
            string subscriptionId,
            string providerNamespace,
            string resourceType)
        {
            return HttpContext.HttpScopeAsync<IActionResult>(
                $"{LoggingBaseName}_plan_list_bysubscription",
                async (logger) =>
                {
                    ValidationUtil.IsRequired(subscriptionId);
                    ValidationUtil.IsRequired(providerNamespace);
                    ValidationUtil.IsRequired(resourceType);
                    ValidationUtil.IsTrue(ResourceTypeIsValid(resourceType));
                    ValidationUtil.IsTrue(ResourceProviderIsValid(providerNamespace));

                    var plans = await this.planManager.ListAsync(
                        userId: null, subscriptionId, resourceGroup: null, logger);

                    logger.LogInfo("plan_list_by_subscription_success");

                    // Required response format.
                    return CreateResponse(HttpStatusCode.OK, plans);
                },
                (ex, logger) => CreateErrorResponse("GetResourceListFailed"),
                swallowException: true);
        }

        /// <summary>
        /// Gets a VSO Plan object.
        /// </summary>
        /// <returns>Returns a Http status code and a VSO SkuPlan object.</returns>
        [HttpGet("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}/resourceReadValidate")]
        public Task<IActionResult> GetValidate(
            string subscriptionId,
            string resourceGroup,
            string providerNamespace,
            string resourceType,
            string resourceName)
        {
            return HttpContext.HttpScopeAsync<IActionResult>(
                $"{LoggingBaseName}_get_validate",
                (logger) =>
                {
                    // Used for pre-read validation only. The Resource is returned from
                    // RPSaaS(MetaRP) CosmosDB storage and not from here
                    return Task.FromResult((IActionResult)Ok());
                });
        }

        private static bool ResourceTypeIsValid(string resourceType)
        {
            return PlanResourceType.Equals(resourceType, StringComparison.InvariantCultureIgnoreCase);
        }

        private static bool ResourceProviderIsValid(string resourceProvider)
        {
            return resourceProvider.Equals(ResourceType, StringComparison.InvariantCultureIgnoreCase);
        }

        private static JsonResult CreateResponse(HttpStatusCode statusCode, object value)
        {
            ValidationUtil.IsRequired(value);
            var response = new JsonResult(value)
            {
                StatusCode = (int)statusCode,
            };

            return response;
        }

        private static JsonResult CreateErrorResponse(string errorCode, string errorMessage = default)
        {
            var errorResponse = new ResourceProviderErrorResponse
            {
                Error = new ResourceProviderErrorInfo
                {
                    Code = errorCode,
                    Message = errorMessage,
                },
                Status = "Failed",
            };

            return new JsonResult(errorResponse)
            {
                StatusCode = (int)HttpStatusCode.OK,
            };
        }
    }
}
