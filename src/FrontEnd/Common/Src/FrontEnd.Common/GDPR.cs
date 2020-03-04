// <copyright file="GDPR.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEnd.Common
{
    /// <summary>
    /// GDPR action that applies to the property.
    /// Currently supports export action. More actions such as Delete/Nullify may be added in the future, to support property level deletion.
    /// </summary>
    public enum GDPRAction
    {
        /// <summary>
        /// The field can be exported upon user request.
        /// </summary>
        Export,
    }

    /// <summary>
    /// Custom Attribute used to tag GDPR fields that store personal information.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Property,
        AllowMultiple = true)]
    public class GDPR : Attribute
    {
        /// <summary>
        /// Gets or sets gDPR action for the annotated property.
        /// </summary>
        public GDPRAction Action { get; set; }
    }
}
