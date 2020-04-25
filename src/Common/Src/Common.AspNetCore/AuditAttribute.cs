// <copyright file="AuditAttribute.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Audit;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore
{
    /// <summary>
    /// Audit Attribute used to decorate controllers/actions that should be logged.
    /// </summary>
    public class AuditAttribute : ActionFilterAttribute
    {
        private const string TargetResourceIdKey = "AuditAttribute-TargetResourceId";
        private static readonly string HttpContextCurrentUserIdSetKey = $"HttpContextCurrentUserProvider-UserIdSet";

        /// <summary>
        /// Initializes a new instance of the <see cref="AuditAttribute"/> class.
        /// </summary>
        /// <param name="auditEventCategory">Target audit event category.</param>
        /// <param name="targetResourceVariableName">Target target resource id.</param>
        /// <param name="targetResourceName">Target target resource name.</param>
        public AuditAttribute(
            AuditEventCategory auditEventCategory,
            string targetResourceVariableName = null,
            string targetResourceName = null)
        {
            AuditEventCategory = auditEventCategory;
            TargetResourceVariableName = targetResourceVariableName;
            TargetResourceName = targetResourceName;
        }

        private AuditEventCategory AuditEventCategory { get; }

        private string TargetResourceVariableName { get; }

        private string TargetResourceName { get; }

        /// <summary>
        /// Updates the key that should be used to find the `targetResourceId` in http context
        /// when it can't be automatically derived from the query string.
        /// </summary>
        /// <param name="httpContext">Target http context.</param>
        /// <param name="targetResourceId">Target target resource id.</param>
        public static void SetTargetResourceId(HttpContext httpContext, string targetResourceId)
        {
            httpContext.Items[TargetResourceIdKey] = targetResourceId;
        }

        /// <inheritdoc/>
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            var logger = context.HttpContext.GetLogger().NewChildLogger();

            try
            {
                // Pull controller and action names
                var controllerActionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
                var controllerName = controllerActionDescriptor?.ControllerName;
                var actionName = controllerActionDescriptor?.ActionName;
                var operationName = $"{controllerName}_{actionName}";
                var resourceName = TargetResourceName ?? controllerName;

                // Pull users name
                var userId = GetCurrentUserIdSet(context.HttpContext).PreferredUserId;

                // Pull resource if from httpcontext if it was provided, and if not
                // default back to pulling from route.
                var resourceId = context.HttpContext.Items[TargetResourceIdKey] as string;
                if (string.IsNullOrEmpty(resourceId))
                {
                    resourceId = context.RouteData.Values[TargetResourceVariableName ?? "id"] as string;
                }

                // If we couldn't find a resource id throw
                if (string.IsNullOrEmpty(resourceId))
                {
                    resourceId = "Unknown";
                }

                // Derive if successful or not
                var operationResult = OperationResult.Success;
                if (!IsSuccessStatusCode(context.HttpContext.Response.StatusCode)
                    || context.Exception != null)
                {
                    operationResult = OperationResult.Failure;
                }

                // Send off audit event
                logger.Audit(
                    operationName,
                    AuditEventCategory,
                    userId,
                    resourceName,
                    resourceId,
                    operationResult);
            }
            catch (Exception e)
            {
                logger.LogException("audit_error", e);
            }
        }

        private bool IsSuccessStatusCode(int statusCode)
        {
            return (statusCode >= 200) && (statusCode <= 299);
        }

        private UserIdSet GetCurrentUserIdSet(HttpContext httpContext)
        {
            return httpContext?.Items[HttpContextCurrentUserIdSetKey] as UserIdSet;
        }
    }
}
