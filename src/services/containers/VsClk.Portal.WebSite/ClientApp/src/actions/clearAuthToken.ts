import { action } from './middleware/useActionCreator';

import { authService } from '../services/authService';
import { useDispatch } from './middleware/useDispatch';

export const clearAuthTokenActionType = 'async.authentication.clearData';

// Basic actions dispatched for reducers
const clearAuthTokenAction = () => action(clearAuthTokenActionType);

// Types to register with reducers
export type ClearAuthTokenAction = ReturnType<typeof clearAuthTokenAction>;

// Exposed - callable actions that have side-effects
export async function clearAuthToken() {
    const dispatch = useDispatch();
    dispatch(clearAuthTokenAction());
    await authService.logout();
}
