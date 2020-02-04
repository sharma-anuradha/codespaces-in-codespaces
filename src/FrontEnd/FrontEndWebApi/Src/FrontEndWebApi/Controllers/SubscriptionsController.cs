// <copyright file="SubscriptionsController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Middleware;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;

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
        private const string ResourceType = "Microsoft.VSOnline";
        private readonly IPlanManager planManager;
        private readonly IEnvironmentManager environmentManager;
        private readonly ITokenProvider tokenProvider;
        private readonly IMapper mapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionsController"/> class.
        /// </summary>
        /// <param name="planManager">The IPlanManager interface.</param>
        /// <param name="tokenProvider">The ITokenProvider interface.</param>
        /// <param name="mapper">The IMapper interface.</param>
        /// <param name="environmentManager">The IEnvironmentManager interface.</param>
        public SubscriptionsController(
            IPlanManager planManager,
            ITokenProvider tokenProvider,
            IMapper mapper,
            IEnvironmentManager environmentManager)
        {
            this.planManager = planManager;
            this.tokenProvider = tokenProvider;
            this.mapper = mapper;
            this.environmentManager = environmentManager;
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

                    if (!await planManager.IsPlanCreationAllowedAsync(subscriptionId, logger))
                    {
                        logger.LogErrorWithDetail("plan_create_validate_error", "Plan creation is not allowed.");
                        return CreateErrorResponse("ValidateResourceFailed");
                    }

                    if (resource.Properties?.UserId != null)
                    {
                        // TODO: Validate that the user id is valid (may require checking Live Share profile.)
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
        /// <param name="subscriptionId">The Azure subscription identifier.</param>
        /// <param name="resourceGroup">The Azure resource group.</param>
        /// <param name="providerNamespace">The Azure resource provider.</param>
        /// <param name="resourceType">The Azure resource type.</param>
        /// <param name="resourceName">The Azure resource name.</param>
        /// <param name="resource">The PlanResource payload.</param>
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

                    var result = await planManager.CreateAsync(plan, logger);

                    logger.AddVsoPlan(plan.Plan);

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
        [HttpPost("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}/resourceCreationCompleted")]
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
        [HttpPost("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}/resourceDeletionValidate")]
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
                    ValidationUtil.IsTrue(ResourceProviderIsValid(providerNamespace));

                    var plan = new VsoPlanInfo
                    {
                        Name = resourceName,
                        ResourceGroup = resourceGroup,
                        Subscription = subscriptionId,
                    };

                    var environments = await environmentManager.ListAsync(
                        logger, planId: plan.ResourceId);
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

                        logger.AddVsoPlan(plan)
                            .FluentAddValue("Count", $"{nonDeletedEnvironments.Count()}")
                            .LogInfo("plan_delete_environment_delete_success");
                    }

                    var response = await planManager.DeleteAsync(plan, logger);
                    if (!response)
                    {
                        logger.AddVsoPlan(plan).LogError("plan_delete_doesnotexist_error");
                        return Ok();
                    }

                    logger.LogInfo($"plan_delete_success");

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
                    ValidationUtil.IsRequired(subscriptionId);
                    ValidationUtil.IsRequired(resourceGroup);
                    ValidationUtil.IsRequired(providerNamespace);
                    ValidationUtil.IsRequired(resourceType);
                    ValidationUtil.IsTrue(ResourceTypeIsValid(resourceType));
                    ValidationUtil.IsTrue(ResourceProviderIsValid(providerNamespace));

                    var plans = await this.planManager.ListAsync(
                        userIdSet: null, subscriptionId, resourceGroup, name: null, logger);

                    logger.LogInfo("plan_list_by_resourcegroup_success");

                    // Required response format.
                    return CreateResponse(HttpStatusCode.OK, plans);
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
                        userIdSet: null, subscriptionId, resourceGroup: null, name: null, logger);

                    logger.LogInfo("plan_list_by_subscription_success");

                    // Required response format.
                    return CreateResponse(HttpStatusCode.OK, plans);
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
        /// <returns>An Http status code and message object indication success or failure of the validation.</returns>
        [HttpGet("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}/resourceReadValidate")]
        public Task<IActionResult> PlanGetValidateAsync(
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
                    return Task.FromResult<IActionResult>(Ok());
                });
        }

        /// <summary>
        /// Validates updating properties of a plan resource.
        /// </summary>
        /// <param name="subscriptionId">The Azure subscription identifier.</param>
        /// <param name="resourceGroup">The Azure resource group.</param>
        /// <param name="providerNamespace">The Azure resource provider.</param>
        /// <param name="resourceType">The Azure resource type.</param>
        /// <param name="resourceName">The Azure resource name.</param>
        /// <returns>An Http status code and message object indication success or failure of the validation.</returns>
        [HttpPost("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}/resourcePatchValidate")]
        public Task<IActionResult> PlanPatchValidateAsync(
            string subscriptionId,
            string resourceGroup,
            string providerNamespace,
            string resourceType,
            string resourceName)
        {
            return HttpContext.HttpScopeAsync<IActionResult>(
                $"{LoggingBaseName}_patch_validate",
                (logger) =>
                {
                    return Task.FromResult<IActionResult>(Ok());
                });
        }

        /// <summary>
        /// Notifies that properties of a plan were updated.
        /// </summary>
        /// <param name="subscriptionId">The Azure subscription identifier.</param>
        /// <param name="resourceGroup">The Azure resource group.</param>
        /// <param name="providerNamespace">The Azure resource provider.</param>
        /// <param name="resourceType">The Azure resource type.</param>
        /// <param name="resourceName">The Azure resource name.</param>
        /// <returns>An Http status code and message object indication success or failure of the operation.</returns>
        [HttpPost("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}/resourcePatchCompleted")]
        public Task<IActionResult> PlanPatchCompletedAsync(
            string subscriptionId,
            string resourceGroup,
            string providerNamespace,
            string resourceType,
            string resourceName)
        {
            return HttpContext.HttpScopeAsync<IActionResult>(
                $"{LoggingBaseName}_patch_completed",
                (logger) =>
                {
                    return Task.FromResult<IActionResult>(Ok());
                });
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
        [HttpPost("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}/readAllEnvironments")]
        public async Task<IActionResult> PlanReadEnvironmentsAsync(
            string subscriptionId,
            string resourceGroup,
            string providerNamespace,
            string resourceType,
            string resourceName,
            [FromQuery]DateTime? expiration)
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
                    ValidationUtil.IsTrue(ResourceProviderIsValid(providerNamespace));

                    var result = await planManager.ListAsync(userIdSet: null, subscriptionId, resourceGroup, resourceName, logger);

                    var plan = result.SingleOrDefault();
                    if (plan == null)
                    {
                        return CreateErrorResponse("PlanNotFound", "PlanNotFound", HttpStatusCode.NotFound);
                    }

                    var token = tokenProvider.GenerateVsSaaSToken(
                        plan,
                        new[] { PlanAccessTokenScopes.ReadEnvironments, },
                        HttpContext.User.Claims,
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
        [HttpPost("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}/writeEnvironments")]
        public async Task<IActionResult> PlanWriteEnvironmentsAsync(
            string subscriptionId,
            string resourceGroup,
            string providerNamespace,
            string resourceType,
            string resourceName,
            [FromQuery]DateTime? expiration)
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
                    ValidationUtil.IsTrue(ResourceProviderIsValid(providerNamespace));

                    var result = await planManager.ListAsync(userIdSet: null, subscriptionId, resourceGroup, resourceName, logger);

                    var plan = result.SingleOrDefault();
                    if (plan == null)
                    {
                        return CreateErrorResponse("PlanNotFound", "PlanNotFound", HttpStatusCode.NotFound);
                    }

                    var token = tokenProvider.GenerateVsSaaSToken(
                        plan,
                        new[] { PlanAccessTokenScopes.WriteEnvironments, },
                        HttpContext.User.Claims,
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
        [HttpPost("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}/deleteAllEnvironments")]
        public async Task<IActionResult> PlanDeleteEnvironmentsAsync(
            string subscriptionId,
            string resourceGroup,
            string providerNamespace,
            string resourceType,
            string resourceName,
            [FromQuery]DateTime? expiration)
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
                    ValidationUtil.IsTrue(ResourceProviderIsValid(providerNamespace));

                    var result = await planManager.ListAsync(userIdSet: null, subscriptionId, resourceGroup, resourceName, logger);

                    var plan = result.SingleOrDefault();
                    if (plan == null)
                    {
                        return CreateErrorResponse("PlanNotFound", "PlanNotFound", HttpStatusCode.NotFound);
                    }

                    var token = tokenProvider.GenerateVsSaaSToken(
                        plan,
                        new[] { PlanAccessTokenScopes.ReadEnvironments, PlanAccessTokenScopes.DeleteEnvironments, },
                        HttpContext.User.Claims,
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
        [HttpPost("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}/readDelegates")]
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
        [HttpPost("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}/writeDelegates")]
        public async Task<IActionResult> PlanWriteDelegatesAsync(
            string subscriptionId,
            string resourceGroup,
            string providerNamespace,
            string resourceType,
            string resourceName,
            [FromBody]IssueDelegatePlanAccessTokenBody requestBody)
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
                    ValidationUtil.IsTrue(ResourceProviderIsValid(providerNamespace));

                    Requires.NotNull(requestBody, nameof(requestBody));
                    Requires.NotNull(requestBody.Identity, nameof(requestBody.Identity));
                    Requires.NotNull(requestBody.Scope, nameof(requestBody.Scope));

                    var result = await planManager.ListAsync(userIdSet: null, subscriptionId, resourceGroup, resourceName, logger);

                    var plan = result.SingleOrDefault();
                    if (plan == null)
                    {
                        return CreateErrorResponse("PlanNotFound", "PlanNotFound", HttpStatusCode.NotFound);
                    }

                    var scopesArray = string.IsNullOrWhiteSpace(requestBody.Scope)
                        ? Array.Empty<string>()
                        : requestBody.Scope.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    if (scopesArray.Any((scope) => !PlanAccessTokenScopes.ValidScopes.Contains(scope)))
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

                    var token = tokenProvider.GenerateDelegatedVsSaaSToken(
                        plan,
                        scopesArray,
                        requestBody.Identity,
                        sourceArmTokenExpiration,
                        requestBody.Expiration,
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
        [HttpPost("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}/deleteDelegates")]
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
        /// Helper method to determine if the input is a valid resource type.
        /// </summary>
        /// <param name="resourceType">The resource type's name.</param>
        /// <returns>Bool.</returns>
        private static bool ResourceTypeIsValid(string resourceType)
        {
            return PlanResourceType.Equals(resourceType, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Helper method to determine if the input is a valid resource provider.
        /// </summary>
        /// <param name="resourceProvider">The resource provider's name.</param>
        /// <returns>Bool.</returns>
        private static bool ResourceProviderIsValid(string resourceProvider)
        {
            return resourceProvider.Equals(ResourceType, StringComparison.InvariantCultureIgnoreCase);
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
                StatusCode = (int)statusCode,
            };
        }
    }
}
