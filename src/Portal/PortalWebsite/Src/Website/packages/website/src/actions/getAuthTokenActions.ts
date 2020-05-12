import { action } from './middleware/useActionCreator';

import { IUser } from '../interfaces/IUser';

export const getAuthTokenActionType = 'async.authentication.getToken';
export const getAuthTokenSuccessActionType = 'async.authentication.getToken.success';
export const getAuthTokenFailureActionType = 'async.authentication.getToken.failure';

// Basic actions dispatched for reducers
export const getAuthTokenAction = () => action(getAuthTokenActionType);
export const getAuthTokenSuccessAction = (token: string, user?: IUser) =>
    action(getAuthTokenSuccessActionType, { token, user });
export const getAuthTokenFailureAction = (error: Error) => action(getAuthTokenFailureActionType, error);

// Types to register with reducers
export type GetAuthTokenAction = ReturnType<typeof getAuthTokenAction>;
export type GetAuthTokenSuccessAction = ReturnType<typeof getAuthTokenSuccessAction>;
export type GetAuthTokenFailureAction = ReturnType<typeof getAuthTokenFailureAction>;
