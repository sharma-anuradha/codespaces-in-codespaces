import { getAuthToken } from './getAuthToken';

import { useActionCreator } from './middleware/useActionCreator';
import { useDispatch } from './middleware/useDispatch';

import { fetchConfiguration } from './fetchConfiguration';
import { fetchEnvironments } from './fetchEnvironments';
import { getUserInfo } from './getUserInfo';
import { telemetry } from '../utils/telemetry';
import { tryGetGitHubCredentialsLocal } from './getGitHubCredentials';

import { register as registerServiceWorker } from '../serviceWorker';

export const initActionType = 'async.app.init';
export const initActionSuccessType = 'async.app.init.success';
export const initActionFailureType = 'async.app.init.failure';

export async function init() {
    const dispatch = useDispatch();
    const action = useActionCreator();

    dispatch(action(initActionType));
    try {
        const tokenPromise = getAuthToken().then((token) => {
            const { email, preferred_username } = token.account.idTokenClaims;
            const userEmail = email || preferred_username;
            telemetry.setIsInternal(userEmail.includes('@microsoft.com'));

            return token;
        });

        const configurationPromise = fetchConfiguration().then((configuration) => {
            registerServiceWorker({
                liveShareEndpoint: configuration.liveShareEndpoint,
                features: {
                    useSharedConnection: true,
                },
            });

            return configuration;
        });

        await Promise.all([dispatch(configurationPromise), dispatch(tokenPromise)]);
        await Promise.all([dispatch(fetchEnvironments()), dispatch(getUserInfo())]);

        dispatch(tryGetGitHubCredentialsLocal());

        dispatch(action(initActionSuccessType));
    } catch (err) {
        dispatch(action(initActionFailureType, err));
    }
}
