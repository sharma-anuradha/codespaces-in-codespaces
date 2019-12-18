import { isNotNullOrEmpty } from '../../utils/isNotNullOrEmpty';

export class PlanCreationError extends Error {
    constructor(public readonly code: PlanCreationFailureReason, message?: string) {
        super(isNotNullOrEmpty(message) ? message : failureReasonToErrorMessage(code));

        if (Error.captureStackTrace) {
            Error.captureStackTrace(this, PlanCreationError);
        }
    }
}

export enum PlanCreationFailureReason {
    NoSubscription,
    NoResourceGroup,
    NoLocation,
    NewResourceGroupAlreadyExists,
    FailedToCreateResourceGroup,
    ResourceGroupProvisioningNotSuccessful,
    FailedToRegisterResourceProvider,
    NotAuthenticated,
    FailedToCreatePlan,
    FailedToAccessResourceGroup,
}

function failureReasonToErrorMessage(reason: PlanCreationFailureReason) {
    return PlanCreationFailureReason[reason];
}
