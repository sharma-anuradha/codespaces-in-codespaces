import { fetchConfiguration } from './configuration';
import { fetchEnvironments } from './fetchEnvironments';
import { Dispatch, action } from './actionUtils';

export const initActionType = 'async.app.init';
export const initActionSuccessType = 'async.app.init.success';
export const initActionFailureType = 'async.app.init.failure';

export const init = async (dispatch: Dispatch) => {
    dispatch(action(initActionType));

    try {
        await dispatch(fetchConfiguration());
        await dispatch(fetchEnvironments());

        dispatch(action(initActionSuccessType));
    } catch (err) {
        dispatch(action(initActionFailureType, err));
    }
};
