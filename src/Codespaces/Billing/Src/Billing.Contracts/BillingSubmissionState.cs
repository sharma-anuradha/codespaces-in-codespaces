// <copyright file="BillingSubmissionState.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    public enum BillingSubmissionState
    {
        /// <summary>
        /// The Billing summary has not been submitted to the commerce platform
        /// </summary>
        None = 0,

        /// <summary>
        /// The Billing summary has been submitted to the commerce platform
        /// </summary>
        Submitted = 1,

        /// <summary>
        /// The Billing summary has been submitted to the commerce platform but returned an error
        /// </summary>
        Error = 2,

        /// <summary>
        /// The Billing summary will not be submitted to the commerce platform as it's a 0  quanity submission or part of a static environment
        /// </summary>
        NeverSubmit = 3,
    }
}