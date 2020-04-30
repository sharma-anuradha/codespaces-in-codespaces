import { action } from './middleware/useActionCreator';

import { useWebClient } from './middleware/useWebClient';
import { useDispatch } from './middleware/useDispatch';
import { ILocations, ILocation } from '../interfaces/ILocation';
import { useActionContext } from './middleware/useActionContext';

export const getLocationsActionType = 'async.locations.getLocations';
export const getLocationsSuccessActionType = 'async.locations.getLocations.success';
export const getLocationsFailureActionType = 'async.locations.getLocations.failure';

export const getLocationActionType = 'async.locations.getLocation';
export const getLocationSuccessActionType = 'async.locations.getLocation.success';
export const getLocationFailureActionType = 'async.locations.getLocation.failure';

const getLocationsAction = () => action(getLocationsActionType);

const getLocationsSuccessAction = (locations: ILocations) => {
    return action(getLocationsSuccessActionType, { locations });
};

const getLocationsFailureAction = (error: Error) => {
    return action(getLocationsFailureActionType, error);
};

const getLocationAction = () => action(getLocationActionType);

const getLocationSuccessAction = (locations: ILocation) => {
    return action(getLocationSuccessActionType, { locations });
};

const getLocationFailureAction = (error: Error) => {
    return action(getLocationFailureActionType, error);
};

export type GetLocationsAction = ReturnType<typeof getLocationsAction>;
export type GetLocationsSuccessAction = ReturnType<typeof getLocationsSuccessAction>;
export type GetLocationsFailureAction = ReturnType<typeof getLocationsFailureAction>;

export type GetLocationAction = ReturnType<typeof getLocationAction>;
export type GetLocationSuccessAction = ReturnType<typeof getLocationSuccessAction>;
export type GetLocationFailureAction = ReturnType<typeof getLocationFailureAction>;

export const locationsEndpoint = '/api/v1/locations';
export async function getLocations() {
    const dispatch = useDispatch();

    try {
        dispatch(getLocationsAction());

        const webClient = useWebClient();

        const locations = await webClient.request<ILocations>(locationsEndpoint, {}, { requiresAuthentication: false });

        dispatch(getLocationsSuccessAction(locations));
        return locations;
    } catch (err) {
        return dispatch(getLocationsFailureAction(err));
    }
}

export async function getLocation(location: string) {
    const dispatch = useDispatch();

    try {
        dispatch(getLocationAction());

        const locationInfo = await doApiGetRequest<ILocation>(`/locations/${location}`);

        dispatch(getLocationSuccessAction(locationInfo));
        return locationInfo;
    } catch (err) {
        return dispatch(getLocationFailureAction(err));
    }
}

async function doApiGetRequest<T>(endpoint: string) {
    const actionContext = useActionContext();

    const { configuration } = actionContext.state;

    if (!configuration) {
        throw new Error('No configuration set, aborting.');
    }

    const { apiEndpoint } = configuration;

    const webClient = useWebClient();
    const url = new URL(`${apiEndpoint}${endpoint}`);
    return await webClient.get<T>(url.toString(), { retryCount: 2 });
}
