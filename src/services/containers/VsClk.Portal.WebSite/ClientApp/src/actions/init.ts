import { getAuthToken } from './getAuthToken';

import { useActionCreator } from './middleware/useActionCreator';
import { useDispatch } from './middleware/useDispatch';

import { fetchConfiguration } from './fetchConfiguration';
import { fetchEnvironments } from './fetchEnvironments';
import { getUserInfo } from './getUserInfo';
import { setAuthCookie } from '../utils/setAuthCookie';
import { telemetry } from '../utils/telemetry';
import { postServiceWorkerMessage } from '../common/post-message';
import { configureServiceWorker } from '../common/service-worker-messages';

export const initActionType = 'async.app.init';
export const initActionSuccessType = 'async.app.init.success';
export const initActionFailureType = 'async.app.init.failure';

export async function init() {
    const dispatch = useDispatch();
    const action = useActionCreator();

    dispatch(action(initActionType));
    try {
        const configurationPromise = fetchConfiguration().then((configuration) => {
            postServiceWorkerMessage({
                type: configureServiceWorker,
                payload: {
                    liveShareEndpoint: configuration.liveShareEndpoint,
                },
            });

            return configuration;
        });

        const tokenPromise = getAuthToken().then((token) => {
            const { email, preferred_username } = token.account.idTokenClaims;
            const userEmail = email || preferred_username;
            telemetry.setIsInternal(userEmail.includes('@microsoft.com'));

            // Fire & forget
            setAuthCookie(token.accessToken);

            return token;
        });

        await Promise.all([dispatch(configurationPromise), dispatch(tokenPromise)]);
        await Promise.all([dispatch(fetchEnvironments()), dispatch(getUserInfo())]);

        dispatch(action(initActionSuccessType));
    } catch (err) {
        dispatch(action(initActionFailureType, err));
    }
}
