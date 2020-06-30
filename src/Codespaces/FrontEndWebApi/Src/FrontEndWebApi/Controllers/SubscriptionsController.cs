﻿// <copyright file="SubscriptionsController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.AspNetCore.Diagnostics.Middleware;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Subscriptions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Constants;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Middleware;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Partners;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Controllers
{
    /// <summary>
    /// The VSO Plan api called by RPSaaS.
    /// </summary>
    [ApiController]
    [Authorize(AuthenticationSchemes = AuthenticationBuilderRPSaasExtensions.AuthenticationScheme)]
    [FriendlyExceptionFilter]
    [Route(ServiceConstants.ApiV1Route)]
    [LoggingBaseName(LoggingBaseName)]
    public class SubscriptionsController : ControllerBase
    {
        private const string LoggingBaseName = "subscriptions_controller";
        private const string PlanResourceType = "plans";
        private readonly IPlanManager planManager;
        private readonly IEnvironmentManager environmentManager;
        private readonly ITokenProvider tokenProvider;
        private readonly IMapper mapper;
        private readonly ISystemConfiguration systemConfiguration;
        private readonly ISubscriptionManager subscriptionManager;
        private readonly ICurrentUserProvider currentUserProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionsController"/> class.
        /// </summary>
        /// <param name="planManager">The IPlanManager interface.</param>
        /// <param name="tokenProvider">The ITokenProvider interface.</param>
        /// <param name="mapper">The IMapper interface.</param>
        /// <param name="environmentManager">The IEnvironmentManager interface.</param>
        /// <param name="systemConfiguration">The ISystemConfiguration interface.</param>
        /// <param name="subscriptionManager">The ISubscriptionManager interface.</param>
        /// <param name="currentUserProvider">Current user provider.</param>
        public SubscriptionsController(
            IPlanManager planManager,
            ITokenProvider tokenProvider,
            IMapper mapper,
            IEnvironmentManager environmentManager,
            ISystemConfiguration systemConfiguration,
            ISubscriptionManager subscriptionManager,
            ICurrentUserProvider currentUserProvider)
        {
            this.planManager = planManager;
            this.tokenProvider = tokenProvider;
            this.mapper = mapper;
            this.environmentManager = environmentManager;
            this.systemConfiguration = systemConfiguration;
            this.subscriptionManager = subscriptionManager;
            this.currentUserProvider = currentUserProvider;
        }

        /// <summary>
        /// This method will be called by RPSaaS before they create the resource in their DB to validate inputs.
        /// </summary>
        /// <param name="subscriptionId">The Azure subscription identifier.</param>
        /// <param name="resourceGroup">The Azure resource group.</param>
        /// <param name="providerNamespace">The Azure resource provider.</param>
        /// <param name="resourceType">The Azure resource type.</param>
        /// <param name="resourceName">The Azure resource name.</param>
        /// <param name="resource">The PlanResource payload.</param>
        /// <returns>Returns a Http status code and message object indication success or failure of the validation.</returns>
        [ArmThrottlePerUser(nameof(SubscriptionsController), nameof(PlanCreateValidateAsync))]
        [HttpPost("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}/resourceCreationValidate")]
        [MdmMetric(name: MdmMetricConstants.ControlPlaneLatency, metricNamespace: MdmMetricConstants.CodespacesHealthNamespace)]
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
                    }
                    catch (Exception ex)
                    {
                        logger.LogErrorWithDetail("plan_create_validate_error", ex.Message);
                        return CreateErrorResponse("NullParameters");
                    }

                    try
                    {
                        ValidationUtil.IsTrue(ResourceTypeIsValid(resourceType), "Invalid resource type.");
                        ValidationUtil.IsTrue(VsoPlanInfo.TryParseProviderNamespace(ref providerNamespace), "Invalid resource provider namespace.");

                        // UserId is required when creating single user plans, disallowed otherwise.
                        if (providerNamespace == VsoPlanInfo.VsoProviderNamespace)
                        {
                            var userId = !string.IsNullOrEmpty(resource.Properties?.UserId)
                                ? resource.Properties.UserId
                                : HttpContext.User.GetUserIdFromClaims();

                            ValidationUtil.IsRequired(userId, "A userId is required.");
                        }
                        else
                        {
                            ValidationUtil.IsTrue(string.IsNullOrEmpty(resource.Properties?.UserId), "A userId is not supported.");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogErrorWithDetail("plan_create_validate_error", ex.Message);
                        return CreateErrorResponse("ValidateResourceFailed");
                    }

                    if (!await planManager.IsPlanCreationAllowedAsync(providerNamespace, subscriptionId, logger))
                    {
                        logger.LogErrorWithDetail("plan_create_validate_error", "Plan creation is not allowed.");
                        return CreateErrorResponse("ValidateResourceFailed");
                    }

                    var plan = new VsoPlanInfo
                    {
                        Name = resourceName,
                        ResourceGroup = resourceGroup,
                        Subscription = subscriptionId,
                        ProviderNamespace = providerNamespace,
                    };

                    var vsoPlan = mapper.Map<VsoPlan>(resource);
                    vsoPlan.Plan = plan;

                    var arePropertiesValid = await planManager.ArePlanPropertiesValidAsync(vsoPlan, logger);
                    if (!arePropertiesValid)
                    {
                        logger.LogErrorWithDetail("plan_create_validate_error", "Plan properties are not valid.");
                        return CreateErrorResponse("ValidateResourceFailed");
                    }

                    logger.LogInfo("plan_create_validate_success");

                    // Required response format in case validation pass with empty body.
                    return Ok();
                });
        }

        /// <summary>
        /// This method will be called by RPSaaS Service before they create the resource in their DB.
        /// </summary>
        /// <param name="subscriptionId">The Azure subscription identifier.</param>
        /// <param name="resourceGroup">The Azure resource group.</param>
        /// <param name="providerNamespace">The Azure resource provider.</param>
        /// <param name="resourceType">The Azure resource type.</param>
        /// <param name="resourceName">The Azure resource name.</param>
        /// <param name="resource">The PlanResource payload.</param>
        /// <returns>Returns an Http status code and a VSOAccount object.</returns>
        [ArmThrottlePerUser(nameof(SubscriptionsController), nameof(PlanCreateAsync))]
        [HttpPut("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}")]
        [MdmMetric(name: MdmMetricConstants.ControlPlaneLatency, metricNamespace: MdmMetricConstants.CodespacesHealthNamespace)]
        public Task<IActionResult> PlanCreateAsync(
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
                    var partner = await HttpContext.GetPartnerAsync(systemConfiguration, logger);

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
                    ValidationUtil.IsTrue(ResourceTypeIsValid(resourceType));
                    ValidationUtil.IsTrue(VsoPlanInfo.TryParseProviderNamespace(ref providerNamespace));

                    var plan = new VsoPlan
                    {
                        Plan = new VsoPlanInfo
                        {
                            Location = location,
                            Name = resourceName,
                            ResourceGroup = resourceGroup,
                            Subscription = subscriptionId,
                            ProviderNamespace = providerNamespace,
                        },
                        Properties = new VsoPlanProperties
                        {
                            DefaultCodespaceSku = resource.Properties?.DefaultCodespaceSku ?? resource.Properties?.DefaultEnvironmentSku,
                            VnetProperties = resource.Properties.VnetProperties.BuildVsoVnetProperty(),
                        },
                        Partner = partner,
                    };

                    // UserId is required when creating single user plans, disallowed otherwise.
                    if (providerNamespace == VsoPlanInfo.VsoProviderNamespace)
                    {
                        var userId = !string.IsNullOrEmpty(resource.Properties?.UserId)
                            ? resource.Properties.UserId
                            : HttpContext.User.GetUserIdFromClaims();

                        ValidationUtil.IsRequired(userId);
                        plan.UserId = userId;
                    }
                    else
                    {
                        ValidationUtil.IsTrue(string.IsNullOrEmpty(resource.Properties?.UserId));
                    }

                    var subscription = await this.subscriptionManager.GetSubscriptionAsync(subscriptionId, logger.NewChildLogger());

                    var result = await planManager.CreateAsync(plan, subscription, logger);

                    logger.AddVsoPlan(plan);

                    if (result.VsoPlan == null)
                    {
                        logger.LogErrorWithDetail("plan_create_error", $"Plan creation failed with ErrorCode: {result.ErrorCode}");
                        return CreateErrorResponse("ValidateResourceFailed");
                    }

                    // Clear the userId property so it will not be stored on the created ARM resource.
                    // It will only be saved internally by the plan manager.
                    resource.Properties.UserId = null;

                    return CreateResponse(HttpStatusCode.OK, resource);
                },
                (ex, logger) => Task.FromResult(CreateErrorResponse("CreateResourceFailed")),
                swallowException: true);
        }

        /// <summary>
        /// This method will be called by RPSaaS Service after they create the resource in their DB.
        /// This method could be used to start billing.
        /// </summary>
        /// <param name="subscriptionId">The Azure subscription identifier.</param>
        /// <param name="resourceGroup">The Azure resource group.</param>
        /// <param name="providerNamespace">The Azure resource provider.</param>
        /// <param name="resourceType">The Azure resource type.</param>
        /// <param name="resourceName">The Azure resource name.</param>
        /// <returns>Returns a Http status code and message.</returns>
        [ArmThrottlePerUser(nameof(SubscriptionsController), nameof(PlanCreateCompleteAsync))]
        [HttpPost("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}/resourceCreationCompleted")]
        [MdmMetric(name: MdmMetricConstants.ControlPlaneLatency, metricNamespace: MdmMetricConstants.CodespacesHealthNamespace)]
        public Task<IActionResult> PlanCreateCompleteAsync(
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
        /// <param name="subscriptionId">The Azure subscription identifier.</param>
        /// <param name="resourceGroup">The Azure resource group.</param>
        /// <param name="providerNamespace">The Azure resource provider.</param>
        /// <param name="resourceType">The Azure resource type.</param>
        /// <param name="resourceName">The Azure resource name.</param>
        /// <returns>Returns a Http status code and message.</returns>
        [ArmThrottlePerUser(nameof(SubscriptionsController), nameof(PlanDeleteValidateAsync))]
        [HttpPost("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}/resourceDeletionValidate")]
        [MdmMetric(name: MdmMetricConstants.ControlPlaneLatency, metricNamespace: MdmMetricConstants.CodespacesHealthNamespace)]
        public Task<IActionResult> PlanDeleteValidateAsync(
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
                    ValidationUtil.IsTrue(VsoPlanInfo.TryParseProviderNamespace(ref providerNamespace));

                    var planInfo = new VsoPlanInfo
                    {
                        Name = resourceName,
                        ResourceGroup = resourceGroup,
                        Subscription = subscriptionId,
                        ProviderNamespace = providerNamespace,
                    };

                    logger.AddVsoPlanInfo(planInfo);

                    var plan = await planManager.GetAsync(planInfo, logger, includeDeleted: true);

                    if (plan == null)
                    {
                        return CreateErrorResponse("ResourceDeleteFailed", "Plan not found");
                    }

                    logger.AddVsoPlan(plan);

                    var environments = await environmentManager.ListAsync(
                        logger, planId: planInfo.ResourceId);
                    var nonDeletedEnvironments = environments.Where(t => t.State != CloudEnvironmentState.Deleted).ToList();
                    if (nonDeletedEnvironments.Any())
                    {
                        foreach (var environment in nonDeletedEnvironments)
                        {
                            var childLogger = logger.NewChildLogger();

                            _ = Task.Run(async () =>
                                {
                                    try
                                    {
                                        var result = await environmentManager.DeleteAsync(environment, childLogger);
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

                        logger.FluentAddValue("Count", $"{nonDeletedEnvironments.Count()}")
                            .LogInfo("plan_delete_environment_delete_success");
                    }

                    if (!plan.IsDeleted)
                    {
                        plan = await planManager.DeleteAsync(plan, logger);
                        logger.LogInfo($"plan_delete_success");
                    }
                    else
                    {
                        logger.LogWarning("plan_delete_alreadydeleted");
                    }

                    // Required response format in case validation pass with empty body.
                    return Ok();
                },
                (ex, logger) => Task.FromResult(CreateErrorResponse("ResourceDeleteFailed")),
                swallowException: true);
        }

        /// <summary>
        /// Gets a list of VSO Plan objects filtered by the input subscriptionID and resourceGroup.
        /// </summary>
        /// <param name="subscriptionId">The Azure subscription identifier.</param>
        /// <param name="resourceGroup">The Azure resource group.</param>
        /// <param name="providerNamespace">The Azure resource provider.</param>
        /// <param name="resourceType">The Azure resource type.</param>
        /// <param name="resourceList">The resources known to Azure.</param>
        /// <returns>Returns an Http status code and a VSOAccount object.</returns>
        [ArmThrottlePerUser(nameof(SubscriptionsController), nameof(PlanListByResourceGroupAsync))]
        [HttpPost("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/resourceReadBegin")]
        [MdmMetric(name: MdmMetricConstants.ControlPlaneLatency, metricNamespace: MdmMetricConstants.CodespacesHealthNamespace)]
        public Task<IActionResult> PlanListByResourceGroupAsync(
            string subscriptionId,
            string resourceGroup,
            string providerNamespace,
            string resourceType,
            [FromBody] PlanResourceList resourceList)
        {
            return HttpContext.HttpScopeAsync<IActionResult>(
                $"{LoggingBaseName}_plan_list_by_resourcegroup",
                async (logger) =>
                {
                    ValidationUtil.IsRequired(subscriptionId);
                    ValidationUtil.IsRequired(resourceGroup);
                    ValidationUtil.IsRequired(providerNamespace);
                    ValidationUtil.IsRequired(resourceType);
                    ValidationUtil.IsRequired(resourceList);
                    ValidationUtil.IsTrue(ResourceTypeIsValid(resourceType));
                    ValidationUtil.IsTrue(VsoPlanInfo.TryParseProviderNamespace(ref providerNamespace));

                    var plans = await this.planManager.ListAsync(
                        userIdSet: null, providerNamespace, subscriptionId, resourceGroup, name: null, logger);
                    resourceList.Value = FilterPlanResources(resourceList.Value, plans.ToArray());

                    logger.LogInfo("plan_list_by_resourcegroup_success");

                    // Required response format.
                    return CreateResponse(HttpStatusCode.OK, resourceList);
                },
                (ex, logger) => Task.FromResult(CreateErrorResponse("GetResourceListFailed")),
                swallowException: true);
        }

        /// <summary>
        /// Gets a list of VSO Plan objects filtered by the input subscriptionID.
        /// </summary>
        /// <param name="subscriptionId">The Azure subscription identifier.</param>
        /// <param name="providerNamespace">The Azure resource provider.</param>
        /// <param name="resourceType">The Azure resource type.</param>
        /// <param name="resourceList">The resources known to Azure.</param>
        /// <returns>Returns an Http status code and a list of VSO SkuPlan objects filtering by subscriptionID.</returns>
        [ArmThrottlePerUser(nameof(SubscriptionsController), nameof(PlanListBySubscriptionAsync))]
        [HttpPost("{subscriptionId}/providers/{providerNamespace}/{resourceType}/resourceReadBegin")]
        [MdmMetric(name: MdmMetricConstants.ControlPlaneLatency, metricNamespace: MdmMetricConstants.CodespacesHealthNamespace)]
        public Task<IActionResult> PlanListBySubscriptionAsync(
            string subscriptionId,
            string providerNamespace,
            string resourceType,
            [FromBody] PlanResourceList resourceList)
        {
            return HttpContext.HttpScopeAsync<IActionResult>(
                $"{LoggingBaseName}_plan_list_by_subscription",
                async (logger) =>
                {
                    ValidationUtil.IsRequired(subscriptionId);
                    ValidationUtil.IsRequired(providerNamespace);
                    ValidationUtil.IsRequired(resourceType);
                    ValidationUtil.IsRequired(resourceList);
                    ValidationUtil.IsTrue(ResourceTypeIsValid(resourceType));
                    ValidationUtil.IsTrue(VsoPlanInfo.TryParseProviderNamespace(ref providerNamespace));

                    var plans = await planManager.ListAsync(
                        userIdSet: null, providerNamespace, subscriptionId, resourceGroup: null, name: null, logger);
                    resourceList.Value = FilterPlanResources(resourceList.Value, plans.ToArray());

                    logger.LogInfo("plan_list_by_subscription_success");

                    // Required response format.
                    return CreateResponse(HttpStatusCode.OK, resourceList);
                },
                (ex, logger) => Task.FromResult(CreateErrorResponse("GetResourceListFailed")),
                swallowException: true);
        }

        /// <summary>
        /// Gets a VSO Plan object.
        /// </summary>
        /// <param name="subscriptionId">The Azure subscription identifier.</param>
        /// <param name="resourceGroup">The Azure resource group.</param>
        /// <param name="providerNamespace">The Azure resource provider.</param>
        /// <param name="resourceType">The Azure resource type.</param>
        /// <param name="resourceName">The Azure resource name.</param>
        /// <param name="resource">The resource from Azure.</param>
        /// <returns>An Http status code and message object indication success or failure of the validation.</returns>
        [ArmThrottlePerUser(nameof(SubscriptionsController), nameof(PlanGetAsync))]
        [HttpPost("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}/resourceReadBegin")]
        [MdmMetric(name: MdmMetricConstants.ControlPlaneLatency, metricNamespace: MdmMetricConstants.CodespacesHealthNamespace)]
        public Task<IActionResult> PlanGetAsync(
            string subscriptionId,
            string resourceGroup,
            string providerNamespace,
            string resourceType,
            string resourceName,
            [FromBody] PlanResource resource)
        {
            return HttpContext.HttpScopeAsync(
                $"{LoggingBaseName}_get",
                async (logger) =>
                {
                    ValidationUtil.IsRequired(subscriptionId);
                    ValidationUtil.IsRequired(resourceGroup);
                    ValidationUtil.IsRequired(providerNamespace);
                    ValidationUtil.IsRequired(resourceType);
                    ValidationUtil.IsRequired(resourceName);
                    ValidationUtil.IsRequired(resource);
                    ValidationUtil.IsTrue(ResourceTypeIsValid(resourceType));
                    ValidationUtil.IsTrue(VsoPlanInfo.TryParseProviderNamespace(ref providerNamespace));

                    var planInfo = new VsoPlanInfo
                    {
                        Name = resourceName,
                        ResourceGroup = resourceGroup,
                        Subscription = subscriptionId,
                        ProviderNamespace = providerNamespace,
                    };

                    var plan = await planManager.GetAsync(planInfo, logger);
                    var plans = plan != null ? new[] { plan } : Array.Empty<VsoPlan>();
                    resource = FilterPlanResources(new[] { resource }, plans).FirstOrDefault();

                    if (resource == null)
                    {
                        return CreateErrorResponse("GetResourceFailed", "Plan not found", HttpStatusCode.NotFound);
                    }

                    // Required response format.
                    return CreateResponse(HttpStatusCode.OK, resource);
                },
                (ex, logger) => Task.FromResult(CreateErrorResponse("GetResourceFailed", "Internal Server Error", HttpStatusCode.InternalServerError)),
                swallowException: true);
        }

        /// <summary>
        /// Validates updating properties of a plan resource.
        /// </summary>
        /// <param name="subscriptionId">The Azure subscription identifier.</param>
        /// <param name="resourceGroup">The Azure resource group.</param>
        /// <param name="providerNamespace">The Azure resource provider.</param>
        /// <param name="resourceType">The Azure resource type.</param>
        /// <param name="resourceName">The Azure resource name.</param>
        /// <param name="resource">The plan resource.</param>
        /// <returns>An Http status code and message object indication success or failure of the validation.</returns>
        [ArmThrottlePerUser(nameof(SubscriptionsController), nameof(PlanPatchValidateAsync))]
        [HttpPost("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}/resourcePatchValidate")]
        [MdmMetric(name: MdmMetricConstants.ControlPlaneLatency, metricNamespace: MdmMetricConstants.CodespacesHealthNamespace)]
        public Task<IActionResult> PlanPatchValidateAsync(
            string subscriptionId,
            string resourceGroup,
            string providerNamespace,
            string resourceType,
            string resourceName,
            [FromBody] PlanResourceUpdateBody resource)
        {
            return HttpContext.HttpScopeAsync<IActionResult>(
                $"{LoggingBaseName}_patch_validate",
                async (logger) =>
                {
                    ValidationUtil.IsRequired(subscriptionId);
                    ValidationUtil.IsRequired(resourceGroup);
                    ValidationUtil.IsRequired(providerNamespace);
                    ValidationUtil.IsRequired(resourceType);
                    ValidationUtil.IsRequired(resourceName);
                    ValidationUtil.IsTrue(ResourceTypeIsValid(resourceType));
                    ValidationUtil.IsTrue(VsoPlanInfo.TryParseProviderNamespace(ref providerNamespace));

                    var plan = new VsoPlanInfo
                    {
                        Name = resourceName,
                        ResourceGroup = resourceGroup,
                        Subscription = subscriptionId,
                        ProviderNamespace = providerNamespace,
                    };

                    var vsoPlan = mapper.Map<VsoPlan>(resource);
                    vsoPlan.Plan = plan;

                    // Check if plan exists
                    var plans = await planManager.ListAsync(
                        null, providerNamespace, subscriptionId, resourceGroup, resourceName, logger);
                    var currentPlan = plans.SingleOrDefault();
                    if (currentPlan == null)
                    {
                        logger.LogErrorWithDetail("plan_patch_failed", "Plan does not exist.");
                        return CreateErrorResponse("PatchValidateResourceFailed");
                    }

                    var arePropertiesValid = await planManager.ArePlanPropertiesValidAsync(vsoPlan, logger);
                    if (!arePropertiesValid)
                    {
                        logger.LogErrorWithDetail("plan_patch_failed", "Plan properties are not valid.");
                        return CreateErrorResponse("PatchValidateResourceFailed");
                    }

                    return Ok();
                },
                (ex, logger) =>
                {
                    var result = (IActionResult)CreateErrorResponse("PatchFailed");
                    return Task.FromResult(result);
                },
                swallowException: true);
        }

        /// <summary>
        /// Notifies that properties of a plan were updated.
        /// </summary>
        /// <param name="subscriptionId">The Azure subscription identifier.</param>
        /// <param name="resourceGroup">The Azure resource group.</param>
        /// <param name="providerNamespace">The Azure resource provider.</param>
        /// <param name="resourceType">The Azure resource type.</param>
        /// <param name="resourceName">The Azure resource name.</param>
        /// <param name="resource">The plan settings resource.</param>
        /// <returns>An Http status code and message object indication success or failure of the operation.</returns>
        [ArmThrottlePerUser(nameof(SubscriptionsController), nameof(PlanPatchCompletedAsync))]
        [HttpPost("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}/resourcePatchCompleted")]
        [MdmMetric(name: MdmMetricConstants.ControlPlaneLatency, metricNamespace: MdmMetricConstants.CodespacesHealthNamespace)]
        public Task<IActionResult> PlanPatchCompletedAsync(
            string subscriptionId,
            string resourceGroup,
            string providerNamespace,
            string resourceType,
            string resourceName,
            [FromBody] PlanResourceUpdateBody resource)
        {
            return HttpContext.HttpScopeAsync<IActionResult>(
                $"{LoggingBaseName}_plan_patch_completed",
                async (logger) =>
                {
                    ValidationUtil.IsRequired(subscriptionId);
                    ValidationUtil.IsRequired(resourceGroup);
                    ValidationUtil.IsRequired(providerNamespace);
                    ValidationUtil.IsRequired(resourceType);
                    ValidationUtil.IsRequired(resourceName);
                    ValidationUtil.IsTrue(ResourceTypeIsValid(resourceType));
                    ValidationUtil.IsTrue(VsoPlanInfo.TryParseProviderNamespace(ref providerNamespace));

                    var planInfo = new VsoPlanInfo
                    {
                        Name = resourceName,
                        ResourceGroup = resourceGroup,
                        Subscription = subscriptionId,
                        ProviderNamespace = providerNamespace,
                    };

                    var vsoPlan = mapper.Map<VsoPlan>(resource);
                    vsoPlan.Plan = planInfo;

                    logger.AddVsoPlan(vsoPlan);

                    if (!await planManager.ApplyPlanPropertiesChangesAsync(vsoPlan, logger))
                    {
                        logger.LogError($"plan_update_settings_error_{nameof(IPlanManager.ApplyPlanPropertiesChangesAsync)}");
                    }

                    var response = await planManager.UpdatePlanPropertiesAsync(vsoPlan, logger);
                    if (response.VsoPlan == null)
                    {
                        logger.LogError($"plan_update_settings_error_{response.ErrorCode}");
                        return Ok();
                    }

                    logger.LogInfo($"plan_update_settings_success");
                    return Ok();
                },
                (ex, logger) =>
                {
                    var result = (IActionResult)CreateErrorResponse("PatchFailed");
                    return Task.FromResult(result);
                },
                swallowException: true);
        }

        /// <summary>
        /// Gets an access token with `read:allenvironments` scope that authorizes listing others'
        /// environments in a plan.
        /// </summary>
        /// <param name="subscriptionId">The Azure subscription identifier.</param>
        /// <param name="resourceGroup">The Azure resource group.</param>
        /// <param name="providerNamespace">The Azure resource provider.</param>
        /// <param name="resourceType">The Azure resource type.</param>
        /// <param name="resourceName">The Azure resource name.</param>
        /// <param name="expiration">The expiration of the returned token.</param>
        /// <returns>An access token response object, or an error object indicating failure.</returns>
        [ArmThrottlePerUser(nameof(SubscriptionsController), nameof(PlanReadEnvironmentsAsync))]
        [HttpPost("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}/readAllEnvironments")]
        [HttpPost("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}/readAllCodespaces")]
        [MdmMetric(name: MdmMetricConstants.ControlPlaneLatency, metricNamespace: MdmMetricConstants.CodespacesHealthNamespace)]
        public async Task<IActionResult> PlanReadEnvironmentsAsync(
            string subscriptionId,
            string resourceGroup,
            string providerNamespace,
            string resourceType,
            string resourceName,
            [FromQuery] DateTime? expiration)
        {
            return await HttpContext.HttpScopeAsync<IActionResult>(
                $"{LoggingBaseName}_read_all_environments",
                async (logger) =>
                {
                    ValidationUtil.IsRequired(subscriptionId);
                    ValidationUtil.IsRequired(resourceGroup);
                    ValidationUtil.IsRequired(providerNamespace);
                    ValidationUtil.IsRequired(resourceType);
                    ValidationUtil.IsRequired(resourceName);
                    ValidationUtil.IsTrue(ResourceTypeIsValid(resourceType));
                    ValidationUtil.IsTrue(VsoPlanInfo.TryParseProviderNamespace(ref providerNamespace));

                    var plans = await planManager.ListAsync(
                        userIdSet: null, providerNamespace, subscriptionId, resourceGroup, resourceName, logger);

                    var plan = plans.SingleOrDefault();
                    if (plan == null)
                    {
                        return CreateErrorResponse("PlanNotFound", "PlanNotFound", HttpStatusCode.NotFound);
                    }

                    var token = await tokenProvider.GenerateVsSaaSTokenAsync(
                        plan,
                        new[] { PlanAccessTokenScopes.ReadEnvironments, },
                        (ClaimsIdentity)HttpContext.User.Identity,
                        expiration,
                        logger);

                    var response = new PlanAccessToken
                    {
                        AccessToken = token,
                    };

                    return new OkObjectResult(mapper.Map<PlanAccessTokenResult>(response));
                },
                (ex, logger) => Task.FromResult(CreateErrorResponse("GetTokenFailed", "GetTokenFailed", HttpStatusCode.InternalServerError)),
                swallowException: true);
        }

        /// <summary>
        /// Gets an access token with `write:environments` scope that authorizes creating
        /// environments in a plan and deleting one's own environments.
        /// </summary>
        /// <param name="subscriptionId">The Azure subscription identifier.</param>
        /// <param name="resourceGroup">The Azure resource group.</param>
        /// <param name="providerNamespace">The Azure resource provider.</param>
        /// <param name="resourceType">The Azure resource type.</param>
        /// <param name="resourceName">The Azure resource name.</param>
        /// <param name="expiration">The expiration of the returned token.</param>
        /// <returns>An access token response object, or an error object indicating failure.</returns>
        [ArmThrottlePerUser(nameof(SubscriptionsController), nameof(PlanWriteEnvironmentsAsync))]
        [HttpPost("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}/writeEnvironments")]
        [HttpPost("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}/writeCodespaces")]
        [MdmMetric(name: MdmMetricConstants.ControlPlaneLatency, metricNamespace: MdmMetricConstants.CodespacesHealthNamespace)]
        public async Task<IActionResult> PlanWriteEnvironmentsAsync(
            string subscriptionId,
            string resourceGroup,
            string providerNamespace,
            string resourceType,
            string resourceName,
            [FromQuery] DateTime? expiration)
        {
            return await HttpContext.HttpScopeAsync<IActionResult>(
                $"{LoggingBaseName}_write_environments",
                async (logger) =>
                {
                    ValidationUtil.IsRequired(subscriptionId);
                    ValidationUtil.IsRequired(resourceGroup);
                    ValidationUtil.IsRequired(providerNamespace);
                    ValidationUtil.IsRequired(resourceType);
                    ValidationUtil.IsRequired(resourceName);
                    ValidationUtil.IsTrue(ResourceTypeIsValid(resourceType));
                    ValidationUtil.IsTrue(VsoPlanInfo.TryParseProviderNamespace(ref providerNamespace));

                    var plans = await planManager.ListAsync(
                        userIdSet: null, providerNamespace, subscriptionId, resourceGroup, resourceName, logger);

                    var plan = plans.SingleOrDefault();
                    if (plan == null)
                    {
                        return CreateErrorResponse("PlanNotFound", "PlanNotFound", HttpStatusCode.NotFound);
                    }

                    var token = await tokenProvider.GenerateVsSaaSTokenAsync(
                        plan,
                        new[] { PlanAccessTokenScopes.WriteEnvironments, },
                        (ClaimsIdentity)HttpContext.User.Identity,
                        expiration,
                        logger);

                    var response = new PlanAccessToken
                    {
                        AccessToken = token,
                    };

                    return new OkObjectResult(mapper.Map<PlanAccessTokenResult>(response));
                },
                (ex, logger) => Task.FromResult(CreateErrorResponse("GetTokenFailed", "GetTokenFailed", HttpStatusCode.InternalServerError)),
                swallowException: true);
        }

        /// <summary>
        /// Gets an access token with `read:allenvironments` and `delete:allenvironments` scopes that
        /// authorizes listing and deleting others' environments in a plan.
        /// </summary>
        /// <param name="subscriptionId">The Azure subscription identifier.</param>
        /// <param name="resourceGroup">The Azure resource group.</param>
        /// <param name="providerNamespace">The Azure resource provider.</param>
        /// <param name="resourceType">The Azure resource type.</param>
        /// <param name="resourceName">The Azure resource name.</param>
        /// <param name="expiration">The expiration of the returned token.</param>
        /// <returns>An access token response object, or an error object indicating failure.</returns>
        [ArmThrottlePerUser(nameof(SubscriptionsController), nameof(PlanDeleteEnvironmentsAsync))]
        [HttpPost("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}/deleteAllEnvironments")]
        [HttpPost("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}/deleteAllCodespaces")]
        [MdmMetric(name: MdmMetricConstants.ControlPlaneLatency, metricNamespace: MdmMetricConstants.CodespacesHealthNamespace)]
        public async Task<IActionResult> PlanDeleteEnvironmentsAsync(
            string subscriptionId,
            string resourceGroup,
            string providerNamespace,
            string resourceType,
            string resourceName,
            [FromQuery] DateTime? expiration)
        {
            return await HttpContext.HttpScopeAsync<IActionResult>(
                $"{LoggingBaseName}_delete_environments",
                async (logger) =>
                {
                    ValidationUtil.IsRequired(subscriptionId);
                    ValidationUtil.IsRequired(resourceGroup);
                    ValidationUtil.IsRequired(providerNamespace);
                    ValidationUtil.IsRequired(resourceType);
                    ValidationUtil.IsRequired(resourceName);
                    ValidationUtil.IsTrue(ResourceTypeIsValid(resourceType));
                    ValidationUtil.IsTrue(VsoPlanInfo.TryParseProviderNamespace(ref providerNamespace));

                    var plans = await planManager.ListAsync(
                        userIdSet: null, providerNamespace, subscriptionId, resourceGroup, resourceName, logger);

                    var plan = plans.SingleOrDefault();
                    if (plan == null)
                    {
                        return CreateErrorResponse("PlanNotFound", "PlanNotFound", HttpStatusCode.NotFound);
                    }

                    var token = await tokenProvider.GenerateVsSaaSTokenAsync(
                        plan,
                        new[] { PlanAccessTokenScopes.ReadEnvironments, PlanAccessTokenScopes.DeleteEnvironments, },
                        (ClaimsIdentity)HttpContext.User.Identity,
                        expiration,
                        logger);

                    var response = new PlanAccessToken
                    {
                        AccessToken = token,
                    };

                    return new OkObjectResult(mapper.Map<PlanAccessTokenResult>(response));
                },
                (ex, logger) => Task.FromResult(CreateErrorResponse("GetTokenFailed", "GetTokenFailed", HttpStatusCode.InternalServerError)),
                swallowException: true);
        }

        /// <summary>
        /// Gets a list of current access delegations for a plan (not including the actual tokens).
        /// </summary>
        /// <param name="subscriptionId">The Azure subscription identifier.</param>
        /// <param name="resourceGroup">The Azure resource group.</param>
        /// <param name="providerNamespace">The Azure resource provider.</param>
        /// <param name="resourceType">The Azure resource type.</param>
        /// <param name="resourceName">The Azure resource name.</param>
        /// <returns>A delegates list response object, or an error object indicating failure.</returns>
        [ArmThrottlePerUser(nameof(SubscriptionsController), nameof(PlanReadDelegatesAsync))]
        [HttpPost("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}/readDelegates")]
        [MdmMetric(name: MdmMetricConstants.ControlPlaneLatency, metricNamespace: MdmMetricConstants.CodespacesHealthNamespace)]
        public Task<IActionResult> PlanReadDelegatesAsync(
            string subscriptionId,
            string resourceGroup,
            string providerNamespace,
            string resourceType,
            string resourceName)
        {
            return HttpContext.HttpScopeAsync<IActionResult>(
                $"{LoggingBaseName}_read_delegates",
                (logger) =>
                {
                    return Task.FromResult<IActionResult>(new StatusCodeResult((int)HttpStatusCode.NotImplemented));
                });
        }

        /// <summary>
        /// Gets a delegated access token with a specified scope that authorizes a delegate user
        /// to access a plan.
        /// </summary>
        /// <param name="subscriptionId">The Azure subscription identifier.</param>
        /// <param name="resourceGroup">The Azure resource group.</param>
        /// <param name="providerNamespace">The Azure resource provider.</param>
        /// <param name="resourceType">The Azure resource type.</param>
        /// <param name="resourceName">The Azure resource name.</param>
        /// <param name="requestBody">The request body.</param>
        /// <returns>An access token response object, or an error object indicating failure.</returns>
        [ArmThrottlePerUser(nameof(SubscriptionsController), nameof(PlanWriteDelegatesAsync))]
        [HttpPost("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}/writeDelegates")]
        [MdmMetric(name: MdmMetricConstants.ControlPlaneLatency, metricNamespace: MdmMetricConstants.CodespacesHealthNamespace)]
        public async Task<IActionResult> PlanWriteDelegatesAsync(
            string subscriptionId,
            string resourceGroup,
            string providerNamespace,
            string resourceType,
            string resourceName,
            [FromBody] IssueDelegatePlanAccessTokenBody requestBody)
        {
            return await HttpContext.HttpScopeAsync<IActionResult>(
                $"{LoggingBaseName}_write_delegates",
                async (logger) =>
                {
                    ValidationUtil.IsRequired(subscriptionId);
                    ValidationUtil.IsRequired(resourceGroup);
                    ValidationUtil.IsRequired(providerNamespace);
                    ValidationUtil.IsRequired(resourceType);
                    ValidationUtil.IsRequired(resourceName);
                    ValidationUtil.IsTrue(ResourceTypeIsValid(resourceType));
                    ValidationUtil.IsTrue(VsoPlanInfo.TryParseProviderNamespace(ref providerNamespace));

                    Requires.NotNull(requestBody, nameof(requestBody));
                    Requires.NotNull(requestBody.Identity, nameof(requestBody.Identity));
                    Requires.NotNull(requestBody.Scope, nameof(requestBody.Scope));

                    var plans = await planManager.ListAsync(
                        userIdSet: null, providerNamespace, subscriptionId, resourceGroup, resourceName, logger);

                    var plan = plans.SingleOrDefault();
                    if (plan == null)
                    {
                        return CreateErrorResponse("PlanNotFound", "PlanNotFound", HttpStatusCode.NotFound);
                    }

                    var scopesArray = string.IsNullOrWhiteSpace(requestBody.Scope)
                        ? Array.Empty<string>()
                        : requestBody.Scope.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    if (scopesArray.Any((scope) => !PlanAccessTokenScopes.ValidPlanScopes.Contains(scope)))
                    {
                        return CreateErrorResponse("InvalidScope", "InvalidScope", HttpStatusCode.BadRequest);
                    }

                    DateTime? sourceArmTokenExpiration = null;
                    if (HttpContext.Items.TryGetValue(AuthenticationBuilderRPSaasExtensions.SourceArmTokenClaims, out var value) &&
                        value is ClaimsPrincipal sourceArmTokenClaims &&
                        int.TryParse(sourceArmTokenClaims.FindFirstValue(JwtRegisteredClaimNames.Exp), out var secSinceEpoch))
                    {
                        sourceArmTokenExpiration = DateTime.UnixEpoch.AddSeconds(secSinceEpoch);
                    }

                    var partner = await HttpContext.GetPartnerAsync(systemConfiguration, logger);

                    var token = await tokenProvider.GenerateDelegatedVsSaaSTokenAsync(
                        plan,
                        partner,
                        scopesArray,
                        requestBody.Identity,
                        sourceArmTokenExpiration,
                        requestBody.Expiration,
                        requestBody.EnvironmentIds,
                        logger);

                    var response = new PlanAccessToken
                    {
                        AccessToken = token,
                    };

                    return new OkObjectResult(mapper.Map<PlanAccessTokenResult>(response));
                },
                (ex, logger) => Task.FromResult(CreateErrorResponse("GetTokenFailed", "GetTokenFailed", HttpStatusCode.InternalServerError)),
                swallowException: true);
        }

        /// <summary>
        /// Revokes a delegated access token that was previously issued for a plan.
        /// </summary>
        /// <param name="subscriptionId">The Azure subscription identifier.</param>
        /// <param name="resourceGroup">The Azure resource group.</param>
        /// <param name="providerNamespace">The Azure resource provider.</param>
        /// <param name="resourceType">The Azure resource type.</param>
        /// <param name="resourceName">The Azure resource name.</param>
        /// <returns>An Http status code and message object indication success or failure of the operation.</returns>
        [ArmThrottlePerUser(nameof(SubscriptionsController), nameof(PlanDeleteDelegatesAsync))]
        [HttpPost("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}/deleteDelegates")]
        [MdmMetric(name: MdmMetricConstants.ControlPlaneLatency, metricNamespace: MdmMetricConstants.CodespacesHealthNamespace)]
        public Task<IActionResult> PlanDeleteDelegatesAsync(
            string subscriptionId,
            string resourceGroup,
            string providerNamespace,
            string resourceType,
            string resourceName)
        {
            return HttpContext.HttpScopeAsync<IActionResult>(
                $"{LoggingBaseName}_delete_delegates",
                (logger) =>
                {
                    return Task.FromResult<IActionResult>(new StatusCodeResult((int)HttpStatusCode.NotImplemented));
                });
        }

        /// <summary>
        /// Handles Subscription Lifecycle notifications.
        /// // https://armwiki.azurewebsites.net/rpaas/sublifecycle.html.
        /// </summary>
        /// <param name="subscriptionId">subscription Id.</param>
        /// <param name="providerNamespace">resource provider namespace.</param>
        /// <param name="resourceType">resource type.</param>
        /// <param name="rpSubscriptionNotification">JSON body request.</param>
        /// <returns>IActionResult.</returns>
        [HttpPut("{subscriptionId}/providers/{providerNamespace}/{resourceType}/SubscriptionLifeCycleNotification")]
        [MdmMetric(name: MdmMetricConstants.ControlPlaneLatency, metricNamespace: MdmMetricConstants.CodespacesHealthNamespace)]
        public Task<IActionResult> OnSubscriptionLifeCycleNotification(
            string subscriptionId,
            string providerNamespace,
            string resourceType,
            [FromBody] RPSubscriptionNotification rpSubscriptionNotification)
        {
            return HttpContext.HttpScopeAsync<IActionResult>(
                $"{LoggingBaseName}_subscription_notification",
                async (logger) =>
                {
                    ValidationUtil.IsRequired(subscriptionId);
                    ValidationUtil.IsRequired(providerNamespace);
                    ValidationUtil.IsRequired(resourceType);
                    ValidationUtil.IsRequired(rpSubscriptionNotification);
                    ValidationUtil.IsRequired(rpSubscriptionNotification.State);
                    ValidationUtil.IsTrue(ResourceTypeIsValid(resourceType));
                    ValidationUtil.IsTrue(VsoPlanInfo.TryParseProviderNamespace(ref providerNamespace));

                    if (!Enum.TryParse(rpSubscriptionNotification.State, true, out SubscriptionStateEnum subscriptionStateEnum))
                    {
                        logger.AddValue("SubscriptionState", rpSubscriptionNotification.State);
                        logger.LogErrorWithDetail("subscription_state_error", $"Subscription state could not be parsed.");
                        return CreateErrorResponse("InvalidSubscriptionState", "InvalidSubscriptionState", HttpStatusCode.BadRequest);
                    }

                    var subscription = await subscriptionManager.GetSubscriptionAsync(subscriptionId, logger.NewChildLogger());

                    subscription = await subscriptionManager.UpdateSubscriptionStateAsync(subscription, subscriptionStateEnum, logger);

                    return new StatusCodeResult((int)HttpStatusCode.OK);
                },
                (ex, logger) => Task.FromResult(CreateErrorResponse("UpdateSubscriptionFailed")),
                swallowException: true);
        }

        /// <summary>
        /// Helper method to determine if the input is a valid resource type.
        /// </summary>
        /// <param name="resourceType">The resource type's name.</param>
        /// <returns>Bool.</returns>
        private static bool ResourceTypeIsValid(string resourceType)
        {
            return PlanResourceType.Equals(resourceType, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Helper method create a json result from the input values.
        /// </summary>
        /// <param name="statusCode">HttpStatusCode object.</param>
        /// <param name="value">The return value.</param>
        /// <returns>JsonResult.</returns>
        private static JsonResult CreateResponse(HttpStatusCode statusCode, object value)
        {
            ValidationUtil.IsRequired(value);
            var response = new JsonResult(value)
            {
                StatusCode = (int)statusCode,
            };

            return response;
        }

        /// <summary>
        /// Helper method to create a json error response from the input values.
        /// </summary>
        /// <param name="errorCode">The string represented error code.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>JsonResult.</returns>
        private static IActionResult CreateErrorResponse(string errorCode, string errorMessage = default, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return ResourceProviderErrorResponse.Create(errorCode, errorMessage, statusCode);
        }

        private static Ownership GetPlanOwnership(VsoPlan plan, UserIdSet userIdSet, bool isMsa)
        {
            if (plan.UserId == null)
            {
                // Multi-user plan: ARM RBAC already allowed the caller list access.
                return Ownership.Shared;
            }
            else if (userIdSet?.EqualsAny(plan.UserId) == true)
            {
                // Single-user plan matches the current user ID or a linked ID.
                return Ownership.CurrentUser;
            }
            else if (isMsa && userIdSet?.LinkedUserIds == null)
            {
                // Single-user plan that doesn't match the current user ID,
                // and the user is an MSA with no linked IDs.
                return Ownership.Unknown;
            }
            else
            {
                // Single-user plan that doesn't match the current user ID.
                return Ownership.OtherUser;
            }
        }

        /// <summary>
        /// Filters plan resources to match the DB, and fills in computed resource properties.
        /// </summary>
        private IEnumerable<PlanResource> FilterPlanResources(
            IEnumerable<PlanResource> resources, ICollection<VsoPlan> plans)
        {
            var userIdSet = this.currentUserProvider.CurrentUserIdSet;
            var isMsa = AuthenticationBuilderRPSaasExtensions.IsArmMsaIdentity(
                HttpContext.User?.Identities.FirstOrDefault());
            resources = resources.Select((resource) =>
            {
                var plan = plans.FirstOrDefault((p) => p.Plan.ResourceId == resource.Id);
                if (plan == null)
                {
                    // The plan was not found in the DB.
                    return null;
                }

                if (resource.Properties == null)
                {
                    resource.Properties = new PlanResourceProperties();
                }

                // Return the ownership status via the plan `userId` property. This is a *slight*
                // abuse of that property, but the actual plan user ID can only be set at plan
                // creation time, and this status is related to it, and it's all for a soon-to-be-
                // deprecated ownership system.
                resource.Properties.UserId = GetPlanOwnership(plan, userIdSet, isMsa).ToString();

                return resource;
            }).Where((resource) => resource != null);
            return resources;
        }
    }
}
