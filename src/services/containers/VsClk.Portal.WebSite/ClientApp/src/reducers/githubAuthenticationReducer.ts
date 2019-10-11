import {
    getGitHubCredentialsSuccessActionType,
    GetGitHubCredentialsSuccessAction,
} from '../actions/getGitHubCredentials';

export type GitHubAuthenticationState = {
    gitHubAccessToken: string | null;
};

type AcceptedActions = GetGitHubCredentialsSuccessAction;

export function githubAuthentication(
    state: GitHubAuthenticationState | undefined = { gitHubAccessToken: null },
    action: AcceptedActions
): GitHubAuthenticationState {
    switch (action.type) {
        case getGitHubCredentialsSuccessActionType:
            return {
                gitHubAccessToken: action.payload.accessToken,
            };
        default:
            return state;
    }
}
