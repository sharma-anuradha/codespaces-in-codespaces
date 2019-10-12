import { action } from './middleware/useActionCreator';
import { ServiceAuthenticationError } from './middleware/useWebClient';
import { init } from './init';

import { authService } from '../services/authService';
import { ITokenWithMsalAccount } from '../typings/ITokenWithMsalAccount';
import { useDispatch } from './middleware/useDispatch';

export const loginActionType = 'async.authentication.getToken';
export const loginSuccessActionType = 'async.authentication.getToken.success';
export const loginFailureActionType = 'async.authentication.getToken.failure';

// Basic actions dispatched for reducers
const loginAction = () => action(loginActionType);
const loginSuccessAction = (token: ITokenWithMsalAccount) => action(loginSuccessActionType, { token });
const loginFailureAction = (error: Error) => action(loginFailureActionType, error);

// Types to register with reducers
export type loginAction = ReturnType<typeof loginAction>;
export type loginSuccessAction = ReturnType<typeof loginSuccessAction>;
export type loginFailureAction = ReturnType<typeof loginFailureAction>;

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

        dispatch(init());

        return token;
    } catch (err) {
        dispatch(loginFailureAction(err));
        return undefined;
    }
}
