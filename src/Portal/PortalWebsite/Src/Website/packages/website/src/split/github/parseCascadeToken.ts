import jwtDecode from 'jwt-decode';

export interface ICascadeToken {
    readonly name: string;
    readonly preferred_username: string;
    readonly plan: string;
    readonly exp: number;
    readonly aud: string;
}

export const isValidCascadeToken = (token: unknown) => {
    if (typeof token !== 'object' || !token) {
        return null;
    }

    const typedToken = token as ICascadeToken;

    return typedToken.name && typedToken.preferred_username && typedToken.plan && typedToken.exp && typedToken.aud;
};

export const parseCascadeToken = (jwtToken: string): ICascadeToken | null => {
    try {
        const token = jwtDecode(jwtToken) as { exp: number };
        if (!isValidCascadeToken(token)) {
            return null;
        }

        token.exp = parseInt(`${token.exp}`, 10) * 1000;

        return token as ICascadeToken;
    } catch (e) {
        return null;
    }
};
