export const isJwtTokenWithMicrosoftEmail = (token: unknown) => {
    if (!token) {
        return false;
    }

    const jwtToken = token as { [key: string]: any };

    const { preferred_username } = jwtToken;
    if (typeof preferred_username !== 'string') {
        return false;
    }

    const split = preferred_username.trim().split('@');

    return split[1] === 'microsoft.com';
};
