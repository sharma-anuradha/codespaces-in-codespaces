import { EnvironmentStateInfo, IEnvironment } from 'vso-client-core';

import * as envRegService from '../services/envRegService';
import { useDispatch } from './middleware/useDispatch';
import { stateChangeEnvironmentAction } from './environmentStateChange';
import { ServiceResponseError } from './middleware/useWebClient';
import { environmentErrorCodeToString } from '../utils/environmentUtils';
import { action } from './middleware/useActionCreator';

export const connectEnvironmentActionType = 'async.environments.connectEnvironment';
export const connectEnvironmentSuccessActionType = 'async.environments.connectEnvironment.success';
export const connectEnvironmentFailureActionType = 'async.environments.connectEnvironment.failure';

// Basic actions dispatched for reducers
export const connectEnvironmentAction = (environmentId: string) =>
    action(connectEnvironmentActionType, { environmentId });
export const connectEnvironmentSuccessAction = (environmentId: string) =>
    action(connectEnvironmentSuccessActionType, { environmentId });
export const connectEnvironmentFailureAction = (environmentId: string, error: Error) =>
    action(connectEnvironmentFailureActionType, { environmentId }, error);

// Exposed - callable actions that have side-effects
export async function connectEnvironment(
    id: string,
    environmentState: EnvironmentStateInfo
): Promise<IEnvironment | undefined> {
    // 1. Try to connect environment
    const dispatch = useDispatch();

    dispatch(connectEnvironmentAction(id));
    let isSuspended = environmentState === EnvironmentStateInfo.Shutdown;

    try {
        if (isSuspended) {
            dispatch(stateChangeEnvironmentAction(id, EnvironmentStateInfo.Starting, EnvironmentStateInfo.Shutdown));
        }

        const environment = await envRegService.connectEnvironment(id, environmentState);
        dispatch(connectEnvironmentSuccessAction(id));
        return environment;
    } catch (err) {
        if (err instanceof ServiceResponseError) {
            await updateErrorMessage();

            async function updateErrorMessage() {
                let text = undefined;
                try {
                    text = await err.response.text();
                } catch {
                    return;
                }

                // We have two types of error responses
                // - code
                // - actual error message
                // We'll normalize them after ignite.
                try {
                    const errorCode = JSON.parse(text);
                    if (typeof errorCode !== 'number') {
                        throw new Error();
                    }
                    err.message = environmentErrorCodeToString(errorCode);
                } catch {
                    err.message = text;
                }
            }
        }

        // Noop
        if (isSuspended) {
            // If starting environment failed, put it to right state.
            let e = await envRegService.getEnvironment(id);
            if (e) {
                dispatch(stateChangeEnvironmentAction(id, e.state, EnvironmentStateInfo.Shutdown));
            }
        }

        return dispatch(connectEnvironmentFailureAction(id, err));
    }
}
