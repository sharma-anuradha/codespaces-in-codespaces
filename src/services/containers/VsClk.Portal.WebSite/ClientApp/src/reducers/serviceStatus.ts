import {
    serviceUnavailableAtTheMomentActionType,
    ServiceUnavailableAtTheMoment,
} from '../actions/serviceUnavailable';

type ServiceStatusState = { isServiceAvailable: boolean };
type AcceptedActions = ServiceUnavailableAtTheMoment;

export function serviceStatus(
    state: ServiceStatusState | undefined = { isServiceAvailable: true },
    action: AcceptedActions
): ServiceStatusState {
    switch (action.type) {
        case serviceUnavailableAtTheMomentActionType:
            return {
                isServiceAvailable: false,
            };
        default:
            return state;
    }
}
