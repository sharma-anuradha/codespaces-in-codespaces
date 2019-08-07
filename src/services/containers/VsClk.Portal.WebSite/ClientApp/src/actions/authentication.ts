import { action, Dispatch } from './actionUtils';
import { authService, IToken, AuthenticationError } from '../services/authService';
import { init } from './init';

export const getAuthTokenActionType = 'async.authentication.getToken';
export const getAuthTokenSuccessActionType = 'async.authentication.getToken.success';
export const getAuthTokenFailureActionType = 'async.authentication.getToken.failure';

export const clearAuthTokenActionType = 'async.authentication.clearData';

export const signInActionType = 'async.authentication.getToken';
export const signInSuccessActionType = 'async.authentication.getToken.success';
export const signInFailureActionType = 'async.authentication.getToken.failure';

// Basic actions dispatched for reducers
const getAuthTokenAction = () => action(getAuthTokenActionType);
const getAuthTokenSuccessAction = (token: IToken) =>
    action(getAuthTokenSuccessActionType, { token });
const getAuthTokenFailureAction = (error: Error) =>
    action(getAuthTokenFailureActionType, undefined, error);

const clearAuthTokenAction = () => action(clearAuthTokenActionType);

const signInAction = () => action(signInActionType);
const signInSuccessAction = (token: IToken) => action(signInSuccessActionType, { token });
const signInFailureAction = (error: Error) => action(signInFailureActionType, undefined, error);

// Types to register with reducers
export type GetAuthTokenAction = ReturnType<typeof getAuthTokenAction>;
export type GetAuthTokenSuccessAction = ReturnType<typeof getAuthTokenSuccessAction>;
export type GetAuthTokenFailureAction = ReturnType<typeof getAuthTokenFailureAction>;

export type ClearAuthTokenAction = ReturnType<typeof clearAuthTokenAction>;

export type SignInAction = ReturnType<typeof getAuthTokenAction>;
export type SignInSuccessAction = ReturnType<typeof getAuthTokenSuccessAction>;
export type SignInFailureAction = ReturnType<typeof getAuthTokenFailureAction>;

// Exposed - callable actions that have side-effects
export const getAuthToken = () => async (dispatch: Dispatch) => {
    try {
        dispatch(getAuthTokenAction());

        const token = await authService.getCachedToken();
        if (!token) {
            dispatch(getAuthTokenFailureAction(new AuthenticationError()));
            return undefined;
        }

        dispatch(getAuthTokenSuccessAction(token));

        return token;
    } catch (err) {
        dispatch(getAuthTokenFailureAction(err));
        return undefined;
    }
};

export const clearAuthToken = () => async (dispatch: Dispatch) => {
    dispatch(clearAuthTokenAction());
    await authService.signOut();
};

export const signIn = () => async (dispatch: Dispatch) => {
    try {
        dispatch(signInAction());

        const token = await authService.signIn();
        if (!token) {
            dispatch(signInFailureAction(new AuthenticationError()));
            return undefined;
        }

        dispatch(signInSuccessAction(token));

        dispatch(init);

        return token;
    } catch (err) {
        dispatch(signInFailureAction(err));
        return undefined;
    }
};
