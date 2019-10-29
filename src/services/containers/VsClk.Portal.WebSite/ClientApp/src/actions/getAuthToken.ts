import { ServiceAuthenticationError } from './middleware/useWebClient';
import { authService } from '../services/authService';
import { useDispatch } from './middleware/useDispatch';
import { getAuthTokenAction, getAuthTokenSuccessAction, getAuthTokenFailureAction } from './getAuthTokenActions';
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
    }
    catch (err) {
        return dispatch(getAuthTokenFailureAction(err));
    }
}
