import {
    getAzDevCredentialsSuccessActionType,
    GetAzDevCredentialsSuccessAction,
} from '../actions/getAzDevCredentials';

export type AzDevAuthenticationState = {
    azDevAccessToken: string | null;
};

type AcceptedActions = GetAzDevCredentialsSuccessAction;

export function azDevAuthentication(
    state: AzDevAuthenticationState | undefined = { azDevAccessToken: null },
    action: AcceptedActions
): AzDevAuthenticationState {
    switch (action.type) {
        case getAzDevCredentialsSuccessActionType:
            return {
                azDevAccessToken: action.payload.accessToken,
            };
        default:
            return state;
    }
}
