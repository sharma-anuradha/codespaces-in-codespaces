import { IAuthenticationProvider, IToken } from '../services/authService';
import { Dispatch } from './actionUtils';
import { getAuthToken, clearAuthToken } from './authentication';

export class ReduxAuthenticationProvider implements IAuthenticationProvider {
    constructor(private dispatch: Dispatch) {}

    async getToken(): Promise<IToken | undefined> {
        return await this.dispatch(getAuthToken());
    }

    signOut(): Promise<void> {
        return this.dispatch(clearAuthToken());
    }
}
