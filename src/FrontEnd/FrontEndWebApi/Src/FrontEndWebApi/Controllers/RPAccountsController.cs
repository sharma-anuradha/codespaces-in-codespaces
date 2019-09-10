// <copyright file="RPAccountsController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Accounts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Middleware;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Sku = Microsoft.VsSaaS.Services.CloudEnvironments.Accounts.Sku;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Controllers
{
    /// <summary>
    /// The VSO Account api called by RPSaaS.
    /// </summary>
    [Authorize(Policy = "RPSaaSIdentity", AuthenticationSchemes = AuthenticationBuilderRPSaasExtensions.AuthenticationScheme)]
    [FriendlyExceptionFilter]
    [LoggingBaseName("rpaccounts_controller")]
    public class RPAccountsController : ControllerBase
    {
        private readonly IAccountManager accountManager;

        private readonly ICurrentUserProvider currentUserProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="RPAccountsController"/> class.
        /// </summary>
        public RPAccountsController(IAccountManager accountManager, ICurrentUserProvider currentUserProvider, IMapper mapper)
        {
            this.accountManager = accountManager;
            this.currentUserProvider = currentUserProvider;
        }

        /// <summary>
        /// This method will be called by RPSaaS before they create the resource in their DB to validate inputs.
        /// </summary>
        /// <returns>Returns a Http status code and message object indication success or failure of the validation.</returns>
        [HttpPost]
        [ActionName("OnResourceCreationValidate")]
        public Task<IActionResult> OnResourceCreationValidate(string subscriptionId, string resourceGroup, string providerNamespace, string resourceType, string resourceName)
        {
            var logger = HttpContext.GetLogger();
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
                logger.LogException("Error validating Account Resource parameters before creation", ex);
                var rpErrorResponse = new ResourceProviderErrorResponse
                {
                    Error = new ResourceProviderErrorInfo
                    {
                        Code = "NullParameters",
                        Message = string.Empty,
                    },
                    Status = "Failed",
                };

                // Required response format in case of validation failure.
                return Task.FromResult<IActionResult>(CreateResponse(HttpStatusCode.OK, rpErrorResponse));
            }

            // Required response format in case validation pass with empty body.
            return Task.FromResult<IActionResult>(new OkObjectResult(string.Empty));
        }

        /// <summary>
        /// This method will be called by RPSaaS Service before they create the resource in their DB.
        /// </summary>
        /// <returns>Returns an Http status code and a VSOAccount object</returns>
        [HttpPut]
        [ActionName("OnResourceCreationBegin")]
        public async Task<IActionResult> OnResourceCreationBegin(
            string subscriptionId,
            string resourceGroup,
            string providerNamespace,
            string resourceType,
            string resourceName,
            [FromBody] AccountInput modelInput)
        {
            var logger = HttpContext.GetLogger();

            try
            {
                var accessToken = this.currentUserProvider.GetBearerToken();

                ValidationUtil.IsRequired(subscriptionId);
                ValidationUtil.IsRequired(resourceGroup);
                ValidationUtil.IsRequired(providerNamespace);
                ValidationUtil.IsRequired(resourceType);
                ValidationUtil.IsRequired(resourceName);
                ValidationUtil.IsRequired(modelInput);
                var nospacesLocation = modelInput.Location.Replace(" ", string.Empty);
                ValidationUtil.IsTrue(
                    Enum.TryParse(nospacesLocation, true, out AzureLocation location),
                    $"Invalid location: ${modelInput.Location}");

                var account = new VsoAccount
                {
                    Account = new VsoAccountInfo
                    {
                        Location = location,
                        Name = resourceName,
                        ResourceGroup = resourceGroup,
                        Subscription = subscriptionId,
                    },
                    Plan = new Sku
                    {
                        Name = modelInput.Properties.Plan.Name,
                    },
                };

                await this.accountManager.CreateOrUpdateAsync(account, logger);

                // Required response format.
                return CreateResponse(HttpStatusCode.OK, modelInput);
            }
            catch (Exception ex)
            {
                logger.LogException("Error creating Account Resource", ex);
                var rpErrorResponse = new ResourceProviderErrorResponse
                {
                    Error = new ResourceProviderErrorInfo
                    {
                        Code = "CreateResourceFailed",
                        Message = string.Empty,
                    },
                    Status = "Failed",
                };

                // Required response format in case of validation failure.
                return CreateResponse(HttpStatusCode.BadRequest, rpErrorResponse);
            }
        }

        /// <summary>
        /// This method will be called by RPSaaS Service after they create the resource in their DB.
        /// This method could be used to start billing.
        /// </summary>
        /// <returns>Returns a Http status code and message</returns>
        [HttpPost]
        [ActionName("OnResourceCreationCompleted")]
        public Task<IActionResult> OnResourceCreationCompleted(string subscriptionId, string resourceGroup, string providerNamespace, string resourceType, string resourceName)
        {
            // Do post creation processing here ex: start billing, write billing Events
            // Required response format with empty body.
            return Task.FromResult<IActionResult>(new OkObjectResult(string.Empty));
        }

        /// <summary>
        /// This method will be called by RPSaaS Service before they delete the resource in their DB.
        /// </summary>
        /// <returns>Returns a Http status code and message.</returns>
        [HttpPost]
        [ActionName("OnResourceDeletionValidate")]
        public async Task<IActionResult> OnResourceDeletionValidate(string subscriptionId, string resourceGroup, string providerNamespace, string resourceType, string resourceName)
        {
            var logger = HttpContext.GetLogger();

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

                var response = await this.accountManager.DeleteAsync(account, logger);
                if (!response)
                {
                    return CreateResponse(statusCode: HttpStatusCode.OK, new ResourceProviderErrorResponse
                    {
                        Error = new ResourceProviderErrorInfo
                        {
                            Code = "DeleteFailed",
                            Message = string.Empty,
                        },
                        Status = "Failed",
                    });
                }

                // Required response format in case validation pass with empty body.
                return new OkObjectResult(string.Empty);
            }
            catch (Exception ex)
            {
                logger.LogException("Error deleting Account Resource", ex);
                var rpErrorResponse = new ResourceProviderErrorResponse
                {
                    Error = new ResourceProviderErrorInfo
                    {
                        Code = "DeleteFailed",
                        Message = string.Empty,
                    },
                    Status = "Failed",
                };

                // Required response format in case of validation failure.
                return CreateResponse(statusCode: HttpStatusCode.OK, rpErrorResponse);
            }
        }

        /// <summary>
        /// Gets a list of VSO Account objects filtered by the input subscriptionID and resourceGroup.
        /// </summary>
        /// <returns>Returns an Http status code and a VSOAccount object.</returns>
        [HttpGet]
        [ActionName("OnResourceListGet")]
        public async Task<IActionResult> OnResourceListGet(string subscriptionId, string resourceGroup, string providerNamespace, string resourceType)
        {
            var logger = HttpContext.GetLogger();

            try
            {
                var accessToken = this.currentUserProvider.GetBearerToken();

                ValidationUtil.IsRequired(subscriptionId);
                ValidationUtil.IsRequired(resourceGroup);
                ValidationUtil.IsRequired(providerNamespace);
                ValidationUtil.IsRequired(resourceType);

                var accounts = await this.accountManager.GetListAsync(subscriptionId, resourceGroup, logger);

                // Required response format.
                return CreateResponse(HttpStatusCode.OK, accounts);
            }
            catch (Exception ex)
            {
                logger.LogException("Error retrieving Account Resource list by Subscription and Resource Group", ex);

                var rpErrorResponse = new ResourceProviderErrorResponse
                {
                    Error = new ResourceProviderErrorInfo
                    {
                        Code = "GetResourceListFailed",
                        Message = string.Empty,
                    },
                    Status = "Failed",
                };

                // Required response format in case of validation failure.
                return CreateResponse(HttpStatusCode.OK, rpErrorResponse);
            }
        }

        /// <summary>
        /// Gets a list of VSO Account objects filtered by the input subscriptionID.
        /// </summary>
        /// <returns>Returns an Http status code and a list of VSO Account objects filtering by subscriptionID.</returns>
        [HttpGet]
        [ActionName("OnResourceListGetBySubscription")]
        public async Task<IActionResult> OnResourceListGetBySubscription(string subscriptionId, string providerNamespace, string resourceType)
        {
            var logger = HttpContext.GetLogger();
            try
            {
                var accessToken = this.currentUserProvider.GetBearerToken();

                ValidationUtil.IsRequired(subscriptionId);
                ValidationUtil.IsRequired(providerNamespace);
                ValidationUtil.IsRequired(resourceType);

                var accounts = await this.accountManager.GetListBySubscriptionAsync(subscriptionId, logger);

                // Required response format.
                return CreateResponse(HttpStatusCode.OK, accounts);
            }
            catch (Exception ex)
            {
                logger.LogException("Error retrieving Account Resource list by Subscription", ex);
                var rpErrorResponse = new ResourceProviderErrorResponse
                {
                    Error = new ResourceProviderErrorInfo
                    {
                        Code = "GetResourceListFailed",
                        Message = string.Empty,
                    },
                    Status = "Failed",
                };

                // Required response format in case of validation failure.
                return CreateResponse(HttpStatusCode.OK, rpErrorResponse);
            }
        }

        /// <summary>
        /// Gets a VSO Account object.
        /// </summary>
        /// <returns>Returns a Http status code and a VSO Account object.</returns>
        [HttpGet]
        [ActionName("OnResourceReadValidate")]
        public Task<IActionResult> OnResourceReadValidate(string subscriptionId, string resourceGroup, string providerNamespace, string resourceType, string resourceName)
        {
            // Used for pre-read validation only. The Resource is returned from RPSaaS(MetaRP) CosmosDB storage and not from here
            return Task.FromResult<IActionResult>(new OkObjectResult(string.Empty));
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
    }
}
