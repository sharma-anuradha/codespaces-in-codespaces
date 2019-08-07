import { fetchConfiguration } from './configuration';
import { fetchEnvironments } from './fetchEnvironments';
import { Dispatch, action } from './actionUtils';
import { getAuthToken } from './authentication';

export const initActionType = 'async.app.init';
export const initActionSuccessType = 'async.app.init.success';
export const initActionFailureType = 'async.app.init.failure';

export const init = async (dispatch: Dispatch) => {
    dispatch(action(initActionType));

    try {
        const configFetch = dispatch(fetchConfiguration());
        const environmentFetch = dispatch(getAuthToken()).then(() => dispatch(fetchEnvironments()));

        await Promise.all([configFetch, environmentFetch]);

        dispatch(action(initActionSuccessType));
    } catch (err) {
        dispatch(action(initActionFailureType, err));
    }
};
