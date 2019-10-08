import jwtDecode from 'jwt-decode';
import { TokenType } from '../typings/TokenType';

export const parseJWTToken = (tokenString: string): TokenType => {
    let expTime = 0;
    try {
        const jwtToken = jwtDecode(tokenString) as { exp: number };
        expTime = (jwtToken.exp - 10) * 1000;
    } catch {
        // ignore
    }

    const token = {
        accessToken: tokenString,
        expiresOn: new Date(expTime)
    }

    return token;
}
