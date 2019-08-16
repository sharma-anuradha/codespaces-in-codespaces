using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.VsCloudKernel.Services.EnvReg.Models;
using Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore;
using Microsoft.VsCloudKernel.Services.EnvReg.Repositories;
using Microsoft.VsCloudKernel.Services.EnvReg.WebApi.Middleware;
using Microsoft.VsCloudKernel.Services.Logging;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using VsClk.EnvReg.Models.Errors;
using VsClk.EnvReg.Repositories;
using VsClk.EnvReg.Telemetry;
using Sku = Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore.Sku;

namespace Microsoft.VsCloudKernel.Services.EnvReg.WebApi.Controllers
{   
    [FriendlyExceptionFilter]
    public class AccountController : ControllerBase
    {
        private IAccountManager AccountManager { get; }
        private ICurrentUserProvider CurrentUserProvider { get; }

        public AccountController(IAccountManager accountManager, ICurrentUserProvider currentUserProvider, IMapper mapper)
        {
            AccountManager = accountManager;
            CurrentUserProvider = currentUserProvider;
        }

        [HttpPost]
        [ActionName("OnResourceCreationValidate")]
        public async Task<IActionResult> OnResourceCreationValidate(string subscriptionId, string resourceGroup, string providerNamespace, string resourceType, string resourceName)
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
                        Message = "",
                    },
                    Status = "Failed",
                };

                // Required response format in case of validation failure.
                return CreateResponse(HttpStatusCode.OK, rpErrorResponse);
            }
            
            // Required response format in case validation pass with empty body.
            return new OkObjectResult(string.Empty);
        }

        [HttpPut]
        [ActionName("OnResourceCreationBegin")]
        public async Task<IActionResult> OnResourceCreationBegin(string subscriptionId, string resourceGroup, string providerNamespace, string resourceType, string resourceName, [FromBody] BillingAccountInput modelInput)
        {
            var logger = HttpContext.GetLogger();

            try
            {    
                var accessToken = CurrentUserProvider.GetBearerToken();

                ValidationUtil.IsRequired(subscriptionId);
                ValidationUtil.IsRequired(resourceGroup);
                ValidationUtil.IsRequired(providerNamespace);
                ValidationUtil.IsRequired(resourceType);
                ValidationUtil.IsRequired(resourceName);
                ValidationUtil.IsRequired(modelInput);

                var account = new BillingAccount
                {
                    Account = new BillingAccountInfo
                    {
                        Location = modelInput.Location,
                        Name = resourceName,
                        ResourceGroup = resourceGroup,
                        Subscription = subscriptionId
                    },
                    Plan = new Sku
                    {
                        Name = modelInput.Properties.Plan.Name
                    }
                };

                await AccountManager.CreateOrUpdateAsync(account, logger);

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
                        Message = "",
                    },
                    Status = "Failed",
                };

                // Required response format in case of validation failure.
                return CreateResponse(HttpStatusCode.BadRequest, rpErrorResponse);
            }
            
        }

        [HttpPost]
        [ActionName("OnResourceCreationCompleted")]
        public async Task<IActionResult> OnResourceCreationCompleted(string subscriptionId, string resourceGroup, string providerNamespace, string resourceType, string resourceName)
        {
            // Do post creation processing here ex: start billing, write billing Events

            // Required response format with empty body.
            return new OkObjectResult(string.Empty);
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

                var account = new BillingAccountInfo
                {
                    Name = resourceName,
                    ResourceGroup = resourceGroup,
                    Subscription = subscriptionId
                };

                var response = await AccountManager.DeleteAsync(account, logger);
                if (!response)
                {
                    return CreateResponse(statusCode: HttpStatusCode.OK, new ResourceProviderErrorResponse
                    {
                        Error = new ResourceProviderErrorInfo
                        {
                            Code = "DeleteFailed",
                            Message = "",
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
                        Message = "",
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
                var accessToken = CurrentUserProvider.GetBearerToken();

                ValidationUtil.IsRequired(subscriptionId);
                ValidationUtil.IsRequired(resourceGroup);
                ValidationUtil.IsRequired(providerNamespace);
                ValidationUtil.IsRequired(resourceType);

                var accounts = await AccountManager.GetListAsync(subscriptionId, resourceGroup, logger);

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
                        Message = "",
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
                var accessToken = CurrentUserProvider.GetBearerToken();

                ValidationUtil.IsRequired(subscriptionId);
                ValidationUtil.IsRequired(providerNamespace);
                ValidationUtil.IsRequired(resourceType);

                var accounts = await AccountManager.GetListBySubscriptionAsync(subscriptionId, logger);

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
                        Message = "",
                    },
                    Status = "Failed",
                };

                // Required response format in case of validation failure.
                return CreateResponse(HttpStatusCode.OK, rpErrorResponse);
            }

        }
        [HttpGet]
        [ActionName("OnResourceReadValidate")]
        public async Task<IActionResult> OnResourceReadValidate(string subscriptionId, string resourceGroup, string providerNamespace, string resourceType, string resourceName)
        {
            //Used for pre-read validation only. The Resource is returned from RPSaaS(MetaRP) CosmosDB storage and not from here

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
    }
}
