import { useActionCreator } from './middleware/useActionCreator';
import { useDispatch } from './middleware/useDispatch';

import { fetchConfiguration } from './fetchConfiguration';
import { fetchEnvironments } from './fetchEnvironments';
import { getUserInfo } from './getUserInfo';
import { telemetry } from '../utils/telemetry';
import { tryGetGitHubCredentialsLocal } from './getGitHubCredentials';
import { tryGetAzDevCredentialsLocal } from './getAzDevCredentials';

import { registerServiceWorker } from 'vso-ts-agent';

import { getPlans } from './plans-actions';
import { useActionContext } from './middleware/useActionContext';
import { setCommonAuthTokenAction } from './getAuthTokenActionCommon';

export const initActionType = 'async.app.init';
export const initActionSuccessType = 'async.app.init.success';
export const initActionFailureType = 'async.app.init.failure';

export async function init(getAuthTokenAction: () => Promise<string>) {
    const dispatch = useDispatch();
    const action = useActionCreator();

    setCommonAuthTokenAction(getAuthTokenAction);

    dispatch(action(initActionType));
    try {
        const tokenPromise = getAuthTokenAction().then((token) => {
            const context = useActionContext();
            const { isInternal } = context.state.authentication;
            
            telemetry.setIsInternal(isInternal);
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
