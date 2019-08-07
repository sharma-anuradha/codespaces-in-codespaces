import { IToken } from '../services/authService';

type AuthenticationState = IToken | null;

export function authentication(): AuthenticationState {
    return null;
}
