import { AuthResponse } from '@vs/msal';
import jwtDecode from 'jwt-decode';

import { ITokenWithMsalAccount } from '../typings/ITokenWithMsalAccount';

export function tokenFromTokenResponse(tokenResponse: AuthResponse): ITokenWithMsalAccount {
    const { accessToken, account } = tokenResponse;
    let msTime = 0;
    try {
        const jwtToken = jwtDecode(accessToken) as {
            exp: number;
        };
        msTime = (jwtToken.exp - 10) * 1000;
    }
    catch { /* ignore */ }
    
    const token = {
        accessToken,
        expiresOn: new Date(msTime),
        account,
    };
    return token;
}
