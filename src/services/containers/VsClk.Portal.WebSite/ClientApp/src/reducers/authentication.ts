import { ITokenWithMsalAccount } from '../typings/ITokenWithMsalAccount';

import { LogoutAction, logoutActionType } from '../actions/logout';
import {
    loginAction,
    loginFailureAction,
    loginSuccessAction,
    loginActionType,
    loginFailureActionType,
    loginSuccessActionType,
    loginInteractionRequiredActionType,
    loginInteractionRequiredAction,
} from '../actions/login';
import {
    GetAuthTokenAction,
    GetAuthTokenFailureAction,
    GetAuthTokenSuccessAction,
    getAuthTokenActionType,
    getAuthTokenFailureActionType,
    getAuthTokenSuccessActionType,
} from '../actions/getAuthTokenActions';

type AcceptedActions =
    | GetAuthTokenAction
    | GetAuthTokenFailureAction
    | GetAuthTokenSuccessAction
    | LogoutAction
    | loginAction
    | loginFailureAction
    | loginSuccessAction
    | loginInteractionRequiredAction;

type AuthenticationState = {
    token: ITokenWithMsalAccount | undefined;
    isAuthenticating: boolean;
    isAuthenticated: boolean;
    isInteractionRequired: boolean;
};

const defaultState: AuthenticationState = {
    token: undefined,
    isAuthenticated: false,
    isAuthenticating: true,
    isInteractionRequired: false,
};

export function authentication(
    state: AuthenticationState = defaultState,
    action: AcceptedActions
): AuthenticationState {
    switch (action.type) {
        case loginActionType:
        case getAuthTokenActionType: {
            return {
                ...state,
                isAuthenticating: true,
            };
        }

        case loginFailureActionType:
        case getAuthTokenFailureActionType: {
            return {
                ...state,
                token: undefined,
                isAuthenticated: false,
                isAuthenticating: false,
            };
        }

        case loginSuccessActionType:
        case getAuthTokenSuccessActionType: {
            return {
                token: action.payload.token,
                isAuthenticated: true,
                isAuthenticating: false,
                isInteractionRequired: false,
            };
        }

        case logoutActionType: {
            return {
                isAuthenticated: false,
                isAuthenticating: false,
                token: undefined,
                isInteractionRequired: false,
            };
        }

        case loginInteractionRequiredActionType: {
            return {
                ...defaultState,
                isInteractionRequired: true,
            };
        }

        default: {
            return state;
        }
    }
}
