import { ServiceAuthenticationError } from '../../../actions/middleware/useWebClient';
import { authService } from '../../../services/authService';
import { useDispatch } from '../../../actions/middleware/useDispatch';
import { getAuthTokenAction, getAuthTokenSuccessAction, getAuthTokenFailureAction } from '../../../actions/getAuthTokenActions';

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
