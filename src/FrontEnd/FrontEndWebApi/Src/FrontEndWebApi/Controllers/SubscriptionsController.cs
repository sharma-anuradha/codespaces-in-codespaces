// <copyright file="SubscriptionsController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Accounts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Middleware;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Controllers
{
    /// <summary>
    /// The VSO Account api called by RPSaaS.
    /// </summary>
    [ApiController]
    [Authorize(Policy = "RPSaaSIdentity", AuthenticationSchemes = AuthenticationBuilderRPSaasExtensions.AuthenticationScheme)]
    [FriendlyExceptionFilter]
    [Route(ServiceConstants.ApiV1Route)]
    [LoggingBaseName("subscriptions_controller")]
    public class SubscriptionsController : ControllerBase
    {
        private readonly IAccountManager accountManager;
        private readonly ICurrentUserProvider currentUserProvider;
        private readonly ICloudEnvironmentManager cloudEnvironmentManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionsController"/> class.
        /// </summary>
        public SubscriptionsController(
            IAccountManager accountManager, 
            ICurrentUserProvider currentUserProvider,
            ICloudEnvironmentManager cloudEnvironmentManager)

        {
            this.accountManager = accountManager;
            this.currentUserProvider = currentUserProvider;
            this.cloudEnvironmentManager = cloudEnvironmentManager;
        }

        /// <summary>
        /// This method will be called by RPSaaS before they create the resource in their DB to validate inputs.
        /// </summary>
        /// <returns>Returns a Http status code and message object indication success or failure of the validation.</returns>
        [HttpPost("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}/resourceCreationValidate")]
        public IActionResult OnResourceCreationValidate(
            string subscriptionId,
            string resourceGroup,
            string providerNamespace,
            string resourceType,
            string resourceName,
            [FromBody] AccountResource resource)
        {
            var logger = HttpContext.GetLogger();
            var duration = logger.StartDuration();
            try
            {
                ValidationUtil.IsRequired(subscriptionId);
                ValidationUtil.IsRequired(resourceGroup);
                ValidationUtil.IsRequired(providerNamespace);
                ValidationUtil.IsRequired(resourceType);
                ValidationUtil.IsRequired(resourceName);

                // TODO: Validate required resource.Properties.UserId.
            }
            catch (Exception ex)
            {
                logger.AddDuration(duration)
                        .LogErrorWithDetail("account_create_validate_error", ex.Message);
                return CreateErrorResponse("NullParameters");
            }

            if (resource.Properties != null && resource.Properties.UserId != null)
            {
                // TODO: Validate that the user profile exists.
            }

            logger.AddDuration(duration)
                    .LogInfo("account_create_validate_success");

            // Required response format in case validation pass with empty body.
            return new OkObjectResult(string.Empty);
        }

        /// <summary>
        /// This method will be called by RPSaaS Service before they create the resource in their DB.
        /// </summary>
        /// <returns>Returns an Http status code and a VSOAccount object</returns>
        [HttpPut("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}")]
        public async Task<IActionResult> OnResourceCreationBegin(
            string subscriptionId,
            string resourceGroup,
            string providerNamespace,
            string resourceType,
            string resourceName,
            [FromBody] AccountResource resource)
        {
            var logger = HttpContext.GetLogger();
            var duration = logger.StartDuration();
            try
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

                if (resource.Properties == null)
                {
                    resource.Properties = new AccountResourceProperties();
                }

                var account = new VsoAccount
                {
                    Account = new VsoAccountInfo
                    {
                        Location = location,
                        Name = resourceName,
                        ResourceGroup = resourceGroup,
                        Subscription = subscriptionId,
                    },
                    UserId = resource.Properties.UserId,
                };

                await this.accountManager.CreateOrUpdateAsync(account, logger);

                // Clear the userId property so it will not be stored on the created ARM resource.
                // It will only be saved internally by the account manager.
                resource.Properties.UserId = null;
                logger.AddAccount(account.Account)
                        .AddDuration(duration)
                        .LogInfo("account_create_success");

                // Required response format.
                return CreateResponse(HttpStatusCode.OK, resource);
            }
            catch (Exception ex)
            {
                logger.AddDuration(duration)
                        .LogErrorWithDetail("account_create_error", ex.Message);
                return CreateErrorResponse("CreateResourceFailed");
            }
        }

        /// <summary>
        /// This method will be called by RPSaaS Service after they create the resource in their DB.
        /// This method could be used to start billing.
        /// </summary>
        /// <returns>Returns a Http status code and message</returns>
        [HttpPost("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}/resourceCreationCompleted")]
        public IActionResult OnResourceCreationCompleted(
            string subscriptionId, string resourceGroup, string providerNamespace, string resourceType, string resourceName)
        {
            // Do post creation processing here ex: start billing, write billing Events
            // Required response format with empty body.
            return new OkObjectResult(string.Empty);
        }

        /// <summary>
        /// This method will be called by RPSaaS Service before they delete the resource in their DB.
        /// </summary>
        /// <returns>Returns a Http status code and message.</returns>
        [HttpPost("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}/resourceDeletionValidate")]
        public async Task<IActionResult> OnResourceDeletionValidate(
            string subscriptionId, string resourceGroup, string providerNamespace, string resourceType, string resourceName)
        {
            var logger = HttpContext.GetLogger();
            var duration = logger.StartDuration();
            try
            {
                ValidationUtil.IsRequired(subscriptionId);
                ValidationUtil.IsRequired(resourceGroup);
                ValidationUtil.IsRequired(providerNamespace);
                ValidationUtil.IsRequired(resourceType);
                ValidationUtil.IsRequired(resourceName);

                var account = new VsoAccountInfo
                {
                    Name = resourceName,
                    ResourceGroup = resourceGroup,
                    Subscription = subscriptionId,
                };

                var environments = await cloudEnvironmentManager.ListEnvironmentsAsync(
                    ownerId: null, name: null, account.ResourceId, logger);
                var nonDeletedEnvironments = environments.Where(t => t.State != CloudEnvironmentState.Deleted).ToList();
                if (nonDeletedEnvironments.Any())
                {
                    foreach (var environment in nonDeletedEnvironments)
                    {
                        var childLogger = logger.NewChildLogger()
                                                .AddAccount(account);

                        Task.Run(async () =>
                        {
                            try
                            {
                                var result = await cloudEnvironmentManager.DeleteEnvironmentAsync(environment.Id, environment.OwnerId, childLogger);
                                if (!result)
                                {
                                    childLogger.AddCloudEnvironment(environment)
                                                .AddDuration(duration)
                                                .LogError("account_delete_environment_delete_error");
                                }
                            }
                            catch (Exception ex)
                            {
                                childLogger.AddDuration(duration)
                                            .LogErrorWithDetail("account_delete_environment_delete_error", ex.Message);
                            }
                        });
                    }

                    logger.AddAccount(account)
                            .AddDuration(duration)
                            .FluentAddValue("Count", $"{nonDeletedEnvironments.Count()}")
                            .LogInfo("account_delete_environment_delete_success");
                }

                var response = await this.accountManager.DeleteAsync(account, logger);
                if (!response)
                {
                    logger.AddAccount(account)
                            .AddDuration(duration)
                            .LogError("account_delete_error");
                    return CreateErrorResponse("DeleteFailed");
                }

                logger.AddDuration(duration)
                        .LogInfo($"account_delete_success");

                // Required response format in case validation pass with empty body.
                return new OkObjectResult(string.Empty);
            }
            catch (Exception ex)
            {
                logger.AddDuration(duration)
                        .LogErrorWithDetail("account_delete_error", ex.Message);
                return CreateErrorResponse("DeleteFailed");
            }
        }

        /// <summary>
        /// Gets a list of VSO Account objects filtered by the input subscriptionID and resourceGroup.
        /// </summary>
        /// <returns>Returns an Http status code and a VSOAccount object.</returns>
        [HttpGet("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}")]
        public async Task<IActionResult> OnResourceListGet(
            string subscriptionId, string resourceGroup, string providerNamespace, string resourceType)
        {
            var logger = HttpContext.GetLogger();
            var duration = logger.StartDuration();
            try
            {
                var accessToken = this.currentUserProvider.GetBearerToken();

                ValidationUtil.IsRequired(subscriptionId);
                ValidationUtil.IsRequired(resourceGroup);
                ValidationUtil.IsRequired(providerNamespace);
                ValidationUtil.IsRequired(resourceType);

                var accounts = await this.accountManager.ListAsync(
                    userId: null, subscriptionId, resourceGroup, logger);

                logger.AddDuration(duration)
                       .LogInfo("account_list_by_resourcegroup_success");

                // Required response format.
                return CreateResponse(HttpStatusCode.OK, accounts);
            }
            catch (Exception ex)
            {
                logger.LogErrorWithDetail("account_list_by_resourcegroup_error", ex.Message);
                return CreateErrorResponse("GetResourceListFailed");
            }
        }

        /// <summary>
        /// Gets a list of VSO Account objects filtered by the input subscriptionID.
        /// </summary>
        /// <returns>Returns an Http status code and a list of VSO Account objects filtering by subscriptionID.</returns>
        [HttpGet("{subscriptionId}/providers/{providerNamespace}/{resourceType}")]
        public async Task<IActionResult> OnResourceListGetBySubscription(
            string subscriptionId, string providerNamespace, string resourceType)
        {
            var logger = HttpContext.GetLogger();
            var duration = logger.StartDuration();
            try
            {
                ValidationUtil.IsRequired(subscriptionId);
                ValidationUtil.IsRequired(providerNamespace);
                ValidationUtil.IsRequired(resourceType);

                var accounts = await this.accountManager.ListAsync(
                    userId: null, subscriptionId, resourceGroup: null, logger);

                logger.AddDuration(duration)
                        .LogInfo("account_list_by_subscription_success");

                // Required response format.
                return CreateResponse(HttpStatusCode.OK, accounts);
            }
            catch (Exception ex)
            {
                logger.AddDuration(duration)
                        .LogErrorWithDetail("account_list_by_subscription_error", ex.Message);
                return CreateErrorResponse("GetResourceListFailed");
            }
        }

        /// <summary>
        /// Gets a VSO Account object.
        /// </summary>
        /// <returns>Returns a Http status code and a VSO Account object.</returns>
        [HttpGet("{subscriptionId}/resourceGroups/{resourceGroup}/providers/{providerNamespace}/{resourceType}/{resourceName}/resourceReadValidate")]
        public IActionResult OnResourceReadValidate(
            string subscriptionId, string resourceGroup, string providerNamespace, string resourceType, string resourceName)
        {
            // Used for pre-read validation only. The Resource is returned from RPSaaS(MetaRP) CosmosDB storage and not from here
            return new OkObjectResult(string.Empty);
        }

        /// <summary>
        /// Creates http responses.
        /// </summary>
        /// <param name="statusCode">The status code.</param>
        /// <param name="value">The value.</param>
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
        /// Creates an error response.
        /// </summary>
        /// <param name="statusCode">The status code.</param>
        /// <param name="value">The value.</param>
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
