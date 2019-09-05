import { getAuthToken } from './getAuthToken';

import { useActionCreator } from './middleware/useActionCreator';
import { useDispatch } from './middleware/useDispatch';

import { fetchConfiguration } from './fetchConfiguration';
import { fetchEnvironments } from './fetchEnvironments';
import { getUserInfo } from './getUserInfo';

export const initActionType = 'async.app.init';
export const initActionSuccessType = 'async.app.init.success';
export const initActionFailureType = 'async.app.init.failure';

export async function init() {
    const dispatch = useDispatch();
    const action = useActionCreator();

    dispatch(action(initActionType));
    try {
        await Promise.all([dispatch(fetchConfiguration()), dispatch(getAuthToken())]);
        await Promise.all([dispatch(fetchEnvironments()), dispatch(getUserInfo())]);

        dispatch(action(initActionSuccessType));
    } catch (err) {
        dispatch(action(initActionFailureType, err));
    }
}
