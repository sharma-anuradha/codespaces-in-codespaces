import { action } from './middleware/useActionCreator';
import { useDispatch } from './middleware/useDispatch';

import {
    getStoredAzDevToken,
    AzDevAuthenticationAttempt,
} from '../services/azDevAuthenticationService';
import { IAuthenticationAttempt } from '../services/authenticationServiceBase';

export const getAzDevCredentialsActionType = 'async.azDevCredentials.get';
export const getAzDevCredentialsSuccessActionType = 'async.azDevCredentials.get.success';
export const getAzDevCredentialsFailureActionType = 'async.azDevCredentials.get.failure';
export const getAzDevCredentialsFromCacheSuccessActionType =
    'async.azDevCredentials.get.fromCache.success';

// Basic actions dispatched for reducers
const getAzDevCredentialsAction = () => action(getAzDevCredentialsActionType);
export const getAzDevCredentialsSuccessAction = (accessToken: string) =>
    action(getAzDevCredentialsSuccessActionType, { accessToken });
const getAzDevCredentialsFailureAction = (error: Error) =>
    action(getAzDevCredentialsFailureActionType, error);
export const getAzDevCredentialsFromCacheSuccessAction = (accessToken: string) =>
    action(getAzDevCredentialsFromCacheSuccessActionType, { accessToken });

// Types to register with reducers
export type GetAzDevCredentialsAction = ReturnType<typeof getAzDevCredentials>;
export type GetAzDevCredentialsSuccessAction = ReturnType<typeof getAzDevCredentialsSuccessAction>;
export type GetAzDevCredentialsFailureAction = ReturnType<typeof getAzDevCredentialsFailureAction>;
export type GetAzDevCredentialsFromCacheSuccessAction = ReturnType<
    typeof getAzDevCredentialsFromCacheSuccessAction
>;

// Exposed - callable actions that have side-effects
export async function getAzDevCredentials(
    azDevAuthAttempt?: IAuthenticationAttempt
): Promise<string> {
    const dispatch = useDispatch();

    try {
        dispatch(getAzDevCredentialsAction());

        if (!azDevAuthAttempt) {
            azDevAuthAttempt = new AzDevAuthenticationAttempt();
        }
        const accessToken = await azDevAuthAttempt.authenticate();
        if (!accessToken) {
            throw new Error('AzDev authentication failed.');
        }

        dispatch(getAzDevCredentialsSuccessAction(accessToken));

        return accessToken;
    } catch (err) {
        dispatch(getAzDevCredentialsFailureAction(err));
        throw err;
    }
}

export async function tryGetAzDevCredentialsLocal() {
    const dispatch = useDispatch();

    const accessToken = await getStoredAzDevToken();
    if (!accessToken) {
        return;
    }

    dispatch(getAzDevCredentialsFromCacheSuccessAction(accessToken));
}

export function storeAzDevCredentials(accessToken: string) {
    const dispatch = useDispatch();

    dispatch(getAzDevCredentialsSuccessAction(accessToken));
}
