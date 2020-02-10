import { action } from './middleware/useActionCreator';
import { useDispatch } from './middleware/useDispatch';

import {
    getStoredGitHubToken,
    GithubAuthenticationAttempt,
} from '../services/gitHubAuthenticationService';

export const getGitHubCredentialsActionType = 'async.githubCredentials.get';
export const getGitHubCredentialsSuccessActionType = 'async.githubCredentials.get.success';
export const getGitHubCredentialsFailureActionType = 'async.githubCredentials.get.failure';

// Basic actions dispatched for reducers
const getGitHubCredentialsAction = () => action(getGitHubCredentialsActionType);
export const getGitHubCredentialsSuccessAction = (accessToken: string) =>
    action(getGitHubCredentialsSuccessActionType, { accessToken });
const getGitHubCredentialsFailureAction = (error: Error) =>
    action(getGitHubCredentialsFailureActionType, error);

// Types to register with reducers
export type GetGitHubCredentialsAction = ReturnType<typeof getGitHubCredentials>;
export type GetGitHubCredentialsSuccessAction = ReturnType<
    typeof getGitHubCredentialsSuccessAction
>;
export type GetGitHubCredentialsFailureAction = ReturnType<
    typeof getGitHubCredentialsFailureAction
>;

// Exposed - callable actions that have side-effects
export async function getGitHubCredentials() {
    const dispatch = useDispatch();

    try {
        dispatch(getGitHubCredentialsAction());

        const gitHubAuthAttempt = new GithubAuthenticationAttempt();
        const accessToken = await gitHubAuthAttempt.authenticate();
        if (!accessToken) {
            throw new Error('GitHub authentication failed.');
        }

        dispatch(getGitHubCredentialsSuccessAction(accessToken));

        return accessToken;
    } catch (err) {
        dispatch(getGitHubCredentialsFailureAction(err));
        throw err;
    }
}

export async function tryGetGitHubCredentialsLocal() {
    const dispatch = useDispatch();

    const accessToken = await getStoredGitHubToken();
    if (!accessToken) {
        return;
    }

    dispatch(getGitHubCredentialsSuccessAction(accessToken));
}

export function storeGitHubCredentials(accessToken: string) {
    const dispatch = useDispatch();

    dispatch(getGitHubCredentialsSuccessAction(accessToken));
}
