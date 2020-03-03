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
import { IUser } from '../interfaces/IUser';

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
    token: string | undefined;
    isAuthenticating: boolean;
    isAuthenticated: boolean;
    isInteractionRequired: boolean;
    isInternal: boolean;
    user?: IUser;
};

const defaultState: AuthenticationState = {
    token: undefined,
    isAuthenticated: false,
    isAuthenticating: true,
    isInteractionRequired: false,
    isInternal: false,
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
            const { token, user } = action.payload;

            const isInternal = (user)
                ? !!(user.email && user.email.includes('@microsoft.com'))
                : false;

            return {
                token,
                user,
                isInternal,
                isAuthenticated: true,
                isAuthenticating: false,
                isInteractionRequired: false,
            };
        }

        case logoutActionType: {
            return {
                isAuthenticated: false,
                isAuthenticating: false,
                isInternal: false,
                token: undefined,
                user: undefined,
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
