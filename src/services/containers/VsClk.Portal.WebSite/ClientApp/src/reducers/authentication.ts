import { ITokenWithMsalAccount } from '../typings/ITokenWithMsalAccount';

import { ClearAuthTokenAction, clearAuthTokenActionType } from '../actions/clearAuthToken';
import {
    loginAction,
    loginFailureAction,
    loginSuccessAction,
    loginActionType,
    loginFailureActionType,
    loginSuccessActionType,
} from '../actions/login';
import {
    GetAuthTokenAction,
    GetAuthTokenFailureAction,
    GetAuthTokenSuccessAction,
    getAuthTokenActionType,
    getAuthTokenFailureActionType,
    getAuthTokenSuccessActionType,
} from '../actions/getAuthToken';

type AcceptedActions =
    | GetAuthTokenAction
    | GetAuthTokenFailureAction
    | GetAuthTokenSuccessAction
    | ClearAuthTokenAction
    | loginAction
    | loginFailureAction
    | loginSuccessAction;

type AuthenticationState = {
    token: ITokenWithMsalAccount | undefined;
    isAuthenticating: boolean;
    isAuthenticated: boolean;
};

const defaultState: AuthenticationState = {
    token: undefined,
    isAuthenticated: false,
    isAuthenticating: true,
};

export function authentication(
    state: AuthenticationState = defaultState,
    action: AcceptedActions
): AuthenticationState {
    switch (action.type) {
        case loginActionType:
        case getAuthTokenActionType:
            return {
                ...state,
                isAuthenticating: true,
            };

        case loginFailureActionType:
        case getAuthTokenFailureActionType:
            return {
                token: undefined,
                isAuthenticated: false,
                isAuthenticating: false,
            };

        case loginSuccessActionType:
        case getAuthTokenSuccessActionType:
            return {
                token: action.payload.token,
                isAuthenticated: true,
                isAuthenticating: false,
            };

        case clearAuthTokenActionType:
            return {
                isAuthenticated: false,
                isAuthenticating: false,
                token: undefined,
            };

        default:
            return state;
    }
}
