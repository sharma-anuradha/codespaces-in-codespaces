import JwtDecode from 'jwt-decode';
import { ICascadeToken } from '../interfaces/ICascadeToken';

export const parseCascadeToken = (token: unknown): ICascadeToken => {
    if (!token) {
        throw new Error('No token set.');
    }

    const cascadeToken = JwtDecode(token as string) as ICascadeToken;
    if (!cascadeToken) {
        throw new Error('No token parsed.');
    }

    if (typeof cascadeToken.idp !== 'string') {
        throw new Error('No token `idp` set.');
    }

    cascadeToken.exp = parseInt(`${cascadeToken.exp}`, 10);

    if (typeof cascadeToken.exp !== 'number' || isNaN(cascadeToken.exp)) {
        throw new Error('No token `exp` set.');
    }

    return cascadeToken;
};
