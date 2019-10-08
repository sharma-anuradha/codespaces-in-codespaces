import { TokenType } from '../typings/TokenType';

/**
 * Function to get token expiration time in seconds.
 */
export const getTokenExpiration = (token: TokenType): number => {
    const { expiresOn } = token;
    const seconds = (new Date(expiresOn).getTime() - Date.now()) / 1000;

    return Math.floor(seconds);
}
