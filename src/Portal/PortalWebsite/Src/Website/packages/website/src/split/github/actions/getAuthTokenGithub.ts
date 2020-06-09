import { ServiceAuthenticationError } from '../../../actions/middleware/useWebClient';
import { useDispatch } from '../../../actions/middleware/useDispatch';
import { getAuthTokenAction, getAuthTokenSuccessAction, getAuthTokenFailureAction } from '../../../actions/getAuthTokenActions';
import { authService } from '../authServiceGithub';
import { IUser } from '../../../interfaces/IUser';
import { parseCascadeToken } from '../parseCascadeToken';

export async function getAuthToken() {
    const dispatch = useDispatch();
    try {
        dispatch(getAuthTokenAction());

        let token = await authService.getCachedCodespaceToken();

        if (!token) {
            token = await authService.getCascadeToken();
        }

        if (!token) {
            throw new ServiceAuthenticationError();
        }

        const parsedToken = parseCascadeToken(token);

        if (!parsedToken) {
            throw new ServiceAuthenticationError();
        }

        const user: IUser = {
            name: parsedToken.name,
            username: parsedToken.preferred_username,
            email: parsedToken.preferred_username
        }
        
        dispatch(getAuthTokenSuccessAction(token, user));
        return token;
    }
    catch (err) {
        return dispatch(getAuthTokenFailureAction(err));
    }
}
