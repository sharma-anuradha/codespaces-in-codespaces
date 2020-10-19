// <copyright file="ContinuationJobConst.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts
{
    /// <summary>
    /// Logger const on the continuation job API
    /// </summary>
    public static class ContinuationJobConst
    {
        public const string JobContinuationPayloadStarted = nameof(JobContinuationPayloadStarted);

        public const string JobContinuationHandlerContinueMessage = "job_continuation_handler_continue";

        public const string JobContinuationCompleteMessage = "job_continuation_complete";

        public const string JobContinuationState = nameof(JobContinuationState);

        public const string JobContinuationResultState = nameof(JobContinuationResultState);

        public const string JobContinuationHasResult = nameof(JobContinuationHasResult);

        public const string JobContinuationNextState = nameof(JobContinuationNextState);

        public const string JobContinuationResultIsNull = nameof(JobContinuationResultIsNull);

        public const string JobContinuationIsRunningTimeValid = "ContinuationIsRunningTimeValid";
    }
}
