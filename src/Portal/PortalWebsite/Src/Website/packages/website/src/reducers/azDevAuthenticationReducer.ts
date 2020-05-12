import {
    getAzDevCredentialsSuccessActionType,
    GetAzDevCredentialsSuccessAction,
    getAzDevCredentialsFromCacheSuccessActionType,
    GetAzDevCredentialsFromCacheSuccessAction,
} from '../actions/getAzDevCredentials';

export type AzDevAuthenticationState = {
    azDevAccessToken: string | null;
};

type AcceptedActions = GetAzDevCredentialsSuccessAction | GetAzDevCredentialsFromCacheSuccessAction;

export function azDevAuthentication(
    state: AzDevAuthenticationState | undefined = { azDevAccessToken: null },
    action: AcceptedActions
): AzDevAuthenticationState {
    switch (action.type) {
        case getAzDevCredentialsSuccessActionType:
        case getAzDevCredentialsFromCacheSuccessActionType:
            return {
                azDevAccessToken: action.payload.accessToken,
            };
        default:
            return state;
    }
}
