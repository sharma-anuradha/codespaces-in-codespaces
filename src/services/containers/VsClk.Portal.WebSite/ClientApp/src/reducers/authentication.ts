import { ITokenWithMsalAccount } from '../typings/ITokenWithMsalAccount';

import { ClearAuthTokenAction, clearAuthTokenActionType } from '../actions/clearAuthToken';
import {
    SignInAction,
    SignInFailureAction,
    SignInSuccessAction,
    signInActionType,
    signInFailureActionType,
    signInSuccessActionType,
} from '../actions/signIn';
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
    | SignInAction
    | SignInFailureAction
    | SignInSuccessAction;

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
        case signInActionType:
        case getAuthTokenActionType:
            return {
                ...state,
                isAuthenticating: true,
            };

        case signInFailureActionType:
        case getAuthTokenFailureActionType:
            return {
                token: undefined,
                isAuthenticated: false,
                isAuthenticating: false,
            };

        case signInSuccessActionType:
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
