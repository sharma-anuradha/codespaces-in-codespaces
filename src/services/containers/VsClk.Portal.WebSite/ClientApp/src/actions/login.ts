import { action } from './middleware/useActionCreator';
import { init } from './init';

import { authService } from '../services/authService';
import { ITokenWithMsalAccount } from '../typings/ITokenWithMsalAccount';
import { useDispatch } from './middleware/useDispatch';
import { tokenFromTokenResponse } from '../services/tokenFromTokenResponse';
import { clientApplication } from '../services/msalConfig';
import { ServiceAuthenticationError } from './middleware/useWebClient';
import { getAuthToken } from './getAuthToken';

export const loginActionType = 'async.authentication.login';
export const loginSuccessActionType = 'async.authentication.login.success';
export const loginFailureActionType = 'async.authentication.login.failure';
export const loginInteractionRequiredActionType = 'async.authentication.login.interaction.required';

// Basic actions dispatched for reducers
const loginAction = () => action(loginActionType);
const loginSuccessAction = (token: ITokenWithMsalAccount) =>
    action(loginSuccessActionType, { token });
const loginFailureAction = (error: Error) => action(loginFailureActionType, error);
const loginInteractionRequiredAction = () => action(loginInteractionRequiredActionType);

// Types to register with reducers
export type loginAction = ReturnType<typeof loginAction>;
export type loginSuccessAction = ReturnType<typeof loginSuccessAction>;
export type loginFailureAction = ReturnType<typeof loginFailureAction>;
export type loginInteractionRequiredAction = ReturnType<typeof loginInteractionRequiredAction>;

// Exposed - callable actions that have side-effects
export async function login() {
    const dispatch = useDispatch();
    try {
        dispatch(loginAction());

        const token = await authService.login();
        if (!token) {
            dispatch(loginFailureAction(new ServiceAuthenticationError()));
            return undefined;
        }

        dispatch(loginSuccessAction(token));

        dispatch(init(getAuthToken));

        return token;
    } catch (err) {
        if (err.name === 'InteractionRequiredAuthError') {
            signal2FARequired();
            return;
        }

        dispatch(loginFailureAction(err));
        return undefined;
    }
}

export const complete2FA = async () => {
    const tokenRequest = {
        scopes: ['email openid offline_access api://9db1d849-f699-4cfb-8160-64bed3335c72/All'],
    };

    if (!clientApplication) {
        throw new Error('Initialize MSAL first.');
    }

    const tokenResponse = await clientApplication.acquireTokenPopup(tokenRequest);

    const token = tokenFromTokenResponse(tokenResponse);

    const dispatch = useDispatch();

    dispatch(loginSuccessAction(token));
    dispatch(init(getAuthToken));

    return token;
};

export const signal2FARequired = () => {
    const dispatch = useDispatch();

    dispatch(loginInteractionRequiredAction());
};
