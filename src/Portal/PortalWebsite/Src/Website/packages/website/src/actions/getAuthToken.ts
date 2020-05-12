import { ServiceAuthenticationError } from './middleware/useWebClient';
import { authService } from '../services/authService';
import { useDispatch } from './middleware/useDispatch';
import { getAuthTokenAction, getAuthTokenSuccessAction, getAuthTokenFailureAction } from './getAuthTokenActions';
import { getUserFromMsalToken } from '../utils/getUserFromMsalToken';

// Exposed - callable actions that have side-effects
export async function getAuthToken() {
    const dispatch = useDispatch();
    try {
        dispatch(getAuthTokenAction());
        const token = await authService.getCachedToken();
        if (!token) {
            throw new ServiceAuthenticationError();
        }

        const user = getUserFromMsalToken(token);
        dispatch(getAuthTokenSuccessAction(token.accessToken, user ));
        return token.accessToken;
    }
    catch (err) {
        return dispatch(getAuthTokenFailureAction(err));
    }
}
