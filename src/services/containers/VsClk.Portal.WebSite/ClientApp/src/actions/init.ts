import { fetchConfiguration } from './fetchConfiguration';
import { fetchEnvironments } from './fetchEnvironments';
import { useActionCreator } from './middleware/useActionCreator';
import { getAuthToken } from './getAuthToken';
import { useDispatch } from './middleware/useDispatch';

export const initActionType = 'async.app.init';
export const initActionSuccessType = 'async.app.init.success';
export const initActionFailureType = 'async.app.init.failure';

export async function init() {
    const dispatch = useDispatch();
    const action = useActionCreator();

    dispatch(action(initActionType));
    try {
        const configFetch = dispatch(fetchConfiguration());
        const authenticate = dispatch(getAuthToken());

        await Promise.all([configFetch, authenticate]);

        await dispatch(fetchEnvironments());

        dispatch(action(initActionSuccessType));
    } catch (err) {
        dispatch(action(initActionFailureType, err));
    }
}
