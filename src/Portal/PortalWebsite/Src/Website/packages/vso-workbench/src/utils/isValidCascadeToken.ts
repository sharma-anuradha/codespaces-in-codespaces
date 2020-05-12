import { timeConstants } from 'vso-client-core';
import { parseCascadeToken } from './parseCascadeToken';

export const isValidCascadeToken = (
    token: string,
    expirationMs: number = timeConstants.HOUR_MS
): boolean => {
    try {
        const cascadeToken = parseCascadeToken(token);

        const timeDelta = cascadeToken.exp * 1000 - Date.now();
        if (timeDelta <= expirationMs) {
            return false;
        }

        return true;
    } catch (e) {
        return false;
    }
};
