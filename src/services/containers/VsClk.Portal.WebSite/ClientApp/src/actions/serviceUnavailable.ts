import { action } from './middleware/useActionCreator';

export const unavailableErrorMessage =
    'Due to overwhelming demand, environment creation is temporarily disabled. ' +
    'We’re working hard to accommodate the interest, so please check back soon as we continually onboard more users.';

export const serviceUnavailableAtTheMomentActionType = 'async.plan.getPlans.serviceUnavailable';

export const serviceUnavailableAtTheMoment = () =>
    action(serviceUnavailableAtTheMomentActionType, new Error(unavailableErrorMessage));
export type ServiceUnavailableAtTheMoment = ReturnType<typeof serviceUnavailableAtTheMoment>;
