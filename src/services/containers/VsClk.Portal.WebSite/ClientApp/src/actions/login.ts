import { action } from './middleware/useActionCreator';
import { init } from './init';

import { authService } from '../services/authService';
import { useDispatch } from './middleware/useDispatch';
import { clientApplication } from '../services/msalConfig';
import { ServiceAuthenticationError } from './middleware/useWebClient';
import { getAuthToken } from './getAuthToken';
import { getRandomKey } from 'vso-client-core';
import { IUser } from '../interfaces/IUser';

export const loginActionType = 'async.authentication.login';
export const loginSuccessActionType = 'async.authentication.login.success';
export const loginFailureActionType = 'async.authentication.login.failure';
export const loginInteractionRequiredActionType = 'async.authentication.login.interaction.required';
export const loginRedirectActionType = 'async.authentication.login.redirect';
export const loginRedirectSuccessActionType = 'async.authentication.login.redirect.success';
export const loginRedirectFailureActionType = 'async.authentication.login.redirect.failure';

// Basic actions dispatched for reducers
const loginAction = () => action(loginActionType);
const loginSuccessAction = (token: string, user?: IUser) =>
    action(loginSuccessActionType, { token, user });
const loginFailureAction = (error: Error) => action(loginFailureActionType, error);
const loginInteractionRequiredAction = () => action(loginInteractionRequiredActionType);
const loginRedirectAction = () => action(loginRedirectActionType);
const loginRedirectSuccessAction = () => action(loginRedirectSuccessActionType);
const loginRedirectFailureAction = (error: Error) => action(loginRedirectFailureActionType, error);

// Types to register with reducers
export type loginAction = ReturnType<typeof loginAction>;
export type loginSuccessAction = ReturnType<typeof loginSuccessAction>;
export type loginFailureAction = ReturnType<typeof loginFailureAction>;
export type loginInteractionRequiredAction = ReturnType<typeof loginInteractionRequiredAction>;
export type loginRedirectAction = ReturnType<typeof loginRedirectAction>;
export type loginRedirectSuccessAction = ReturnType<typeof loginRedirectSuccessAction>;
export type loginRedirectFailureAction = ReturnType<typeof loginRedirectFailureAction>;

// Exposed - callable actions that have side-effects
export async function login() {
    const dispatch = useDispatch();
    try {
        dispatch(loginAction());

        await authService.login();
    } catch (err) {
        dispatch(loginFailureAction(err));
        return undefined;
    }
}

export async function loginSilent() {
    const dispatch = useDispatch();
    try {
        const token = await authService.loginSilent();
        if (!token) {
            dispatch(loginFailureAction(new ServiceAuthenticationError()));
            return undefined;
        }

        dispatch(loginSuccessAction(token.accessToken));

        dispatch(init(getAuthToken));

        return token;
    } catch (err) {
        if (err.name === 'InteractionRequiredAuthError') {
            signal2FARequired();
            throw err;
        }

        dispatch(loginFailureAction(err));
        throw err;
    }
}

export function acquireTokenRedirect() {
    const dispatch = useDispatch();
    try {
        dispatch(loginRedirectAction());

        authService.acquireTokenRedirect();

        dispatch(loginRedirectSuccessAction());
    } catch (err) {
        dispatch(loginRedirectFailureAction(err));
        throw err;
    }
}

export const complete2FA = () => {
    const tokenRequest = {
        scopes: ['email openid offline_access api://9db1d849-f699-4cfb-8160-64bed3335c72/All'],
        state: ''
    };
    let state = getRandomKey()?.key.toString('base64');
    if (state) {
        tokenRequest.state = state;
    }

    if (!clientApplication) {
        throw new Error('Initialize MSAL first.');
    }

    clientApplication.acquireTokenRedirect(tokenRequest);
};

export const signal2FARequired = () => {
    const dispatch = useDispatch();

    dispatch(loginInteractionRequiredAction());
};
