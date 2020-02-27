import { useActionCreator } from './middleware/useActionCreator';
import { useDispatch } from './middleware/useDispatch';

import { fetchConfiguration } from './fetchConfiguration';
import { fetchEnvironments } from './fetchEnvironments';
import { getUserInfo } from './getUserInfo';
import { telemetry } from '../utils/telemetry';
import { tryGetGitHubCredentialsLocal } from './getGitHubCredentials';
import { tryGetAzDevCredentialsLocal } from './getAzDevCredentials';

import { register as registerServiceWorker } from '../serviceWorker';
import { getPlans } from './plans-actions';
import { ITokenWithMsalAccount } from '../typings/ITokenWithMsalAccount';

export const initActionType = 'async.app.init';
export const initActionSuccessType = 'async.app.init.success';
export const initActionFailureType = 'async.app.init.failure';

export async function init(getAuthTokenAction: () => Promise<ITokenWithMsalAccount>) {
    const dispatch = useDispatch();
    const action = useActionCreator();

    dispatch(action(initActionType));
    try {
        const tokenPromise = getAuthTokenAction().then((token) => {
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
        await Promise.all([dispatch(getPlans()), dispatch(getUserInfo())]);
        await Promise.all([
            dispatch(fetchEnvironments()),
            dispatch(tryGetGitHubCredentialsLocal()),
            dispatch(tryGetAzDevCredentialsLocal()),
        ]);

        dispatch(action(initActionSuccessType));
    } catch (err) {
        dispatch(action(initActionFailureType, err));
    }
}
