import { ILocations } from '../interfaces/ILocation';

import {
    GetLocationsAction,
    GetLocationsSuccessAction,
    GetLocationsFailureAction,
    getLocationsSuccessActionType,
} from '../actions/locations-actions';

type AcceptedActions =
    | GetLocationsAction
    | GetLocationsSuccessAction
    | GetLocationsFailureAction;

export const defaultLocations: ILocations = {
    current: '',
    available: [],
    hostnames: {},
};

export function locations(
    state: ILocations = defaultLocations,
    action: AcceptedActions
): ILocations {
    switch (action.type) {
        case getLocationsSuccessActionType:
            return {
                ...action.payload.locations
        };
        default:
            return state;
    }
}
