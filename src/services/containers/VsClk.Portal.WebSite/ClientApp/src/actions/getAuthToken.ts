import { action } from './middleware/useActionCreator';
import { ServiceAuthenticationError } from './middleware/useWebClient';

import { authService } from '../services/authService';
import { ITokenWithMsalAccount } from '../typings/ITokenWithMsalAccount';
import { useDispatch } from './middleware/useDispatch';

export const getAuthTokenActionType = 'async.authentication.getToken';
export const getAuthTokenSuccessActionType = 'async.authentication.getToken.success';
export const getAuthTokenFailureActionType = 'async.authentication.getToken.failure';

// Basic actions dispatched for reducers
const getAuthTokenAction = () => action(getAuthTokenActionType);
export const getAuthTokenSuccessAction = (token: ITokenWithMsalAccount) =>
    action(getAuthTokenSuccessActionType, { token });
const getAuthTokenFailureAction = (error: Error) => action(getAuthTokenFailureActionType, error);

// Types to register with reducers
export type GetAuthTokenAction = ReturnType<typeof getAuthTokenAction>;
export type GetAuthTokenSuccessAction = ReturnType<typeof getAuthTokenSuccessAction>;
export type GetAuthTokenFailureAction = ReturnType<typeof getAuthTokenFailureAction>;

// Exposed - callable actions that have side-effects
export async function getAuthToken() {
    const dispatch = useDispatch();
    try {
        dispatch(getAuthTokenAction());

        const token = await authService.getCachedToken();
        if (!token) {
            throw new ServiceAuthenticationError();
        }

        dispatch(getAuthTokenSuccessAction(token));
        return token;
    } catch (err) {
        return dispatch(getAuthTokenFailureAction(err));
    }
}
