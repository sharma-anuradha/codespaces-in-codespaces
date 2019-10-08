import { action } from './middleware/useActionCreator';
import { ServiceAuthenticationError } from './middleware/useWebClient';
import { init } from './init';

import { authService } from '../services/authService';
import { ITokenWithMsalAccount } from '../typings/ITokenWithMsalAccount';
import { useDispatch } from './middleware/useDispatch';

export const signInActionType = 'async.authentication.getToken';
export const signInSuccessActionType = 'async.authentication.getToken.success';
export const signInFailureActionType = 'async.authentication.getToken.failure';

// Basic actions dispatched for reducers
const signInAction = () => action(signInActionType);
const signInSuccessAction = (token: ITokenWithMsalAccount) => action(signInSuccessActionType, { token });
const signInFailureAction = (error: Error) => action(signInFailureActionType, error);

// Types to register with reducers
export type SignInAction = ReturnType<typeof signInAction>;
export type SignInSuccessAction = ReturnType<typeof signInSuccessAction>;
export type SignInFailureAction = ReturnType<typeof signInFailureAction>;

// Exposed - callable actions that have side-effects
export async function signIn() {
    const dispatch = useDispatch();
    try {
        dispatch(signInAction());

        const token = await authService.signIn();
        if (!token) {
            dispatch(signInFailureAction(new ServiceAuthenticationError()));
            return undefined;
        }

        dispatch(signInSuccessAction(token));

        dispatch(init());

        return token;
    } catch (err) {
        dispatch(signInFailureAction(err));
        return undefined;
    }
}
