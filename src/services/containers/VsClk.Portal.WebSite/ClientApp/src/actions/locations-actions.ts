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

const getLocationsSuccessAction = (LocationsList: ILocations) => {
    return action(getLocationsSuccessActionType, { LocationsList });
};

const getLocationsFailureAction = (error: Error) => {
    return action(getLocationsFailureActionType, error);
};

const getLocationAction = () => action(getLocationActionType);

const getLocationSuccessAction = (LocationsList: ILocation) => {
    return action(getLocationSuccessActionType, { LocationsList });
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

export async function getLocations() {
    const dispatch = useDispatch();

    try {
        dispatch(getLocationsAction());

        const locations = await doApiGetRequest<ILocations>(`/locations`);

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

        // TODO: filtering windows on the client side for now to hide the option - later this filter will be move to the server (#983757)
        const skuFilteredLocationInfo = {
            ...locationInfo,
            skus: locationInfo.skus.filter((s) => s.os.toLowerCase() !== 'windows'),
        };

        dispatch(getLocationSuccessAction(skuFilteredLocationInfo));
        return skuFilteredLocationInfo;
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
    return await webClient.get<T>(url.toString());
}
