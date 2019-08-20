// <copyright file="AccountController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Accounts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Middleware;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Newtonsoft.Json;
using Sku = Microsoft.VsSaaS.Services.CloudEnvironments.Accounts.Sku;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Controllers
{
    [FriendlyExceptionFilter]
    public class AccountController : ControllerBase
    {
        private readonly IAccountManager accountManager;

        private readonly ICurrentUserProvider currentUserProvider;

        public AccountController(IAccountManager accountManager, ICurrentUserProvider currentUserProvider, IMapper mapper)
        {
            this.accountManager = accountManager;
            this.currentUserProvider = currentUserProvider;
        }

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

                var account = new VsoAccount
                {
                    Account = new VsoAccountInfo
                    {
                        Location = modelInput.Location,
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

        [HttpPost]
        [ActionName("OnResourceCreationCompleted")]
        public Task<IActionResult> OnResourceCreationCompleted(string subscriptionId, string resourceGroup, string providerNamespace, string resourceType, string resourceName)
        {
            // Do post creation processing here ex: start billing, write billing Events
            // Required response format with empty body.
            return Task.FromResult<IActionResult>(new OkObjectResult(string.Empty));
        }

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
